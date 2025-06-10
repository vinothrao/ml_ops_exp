import json
import azure.functions as func
import logging
import requests
import numpy as np
import os
import pandas as pd
app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)

# Get configuration from environment variables
MLFLOW_ENDPOINT_URL = os.environ["MLFLOW_ENDPOINT_URL"]
client_id = os.environ["DATABRICKS_CLIENT_ID"]
client_secret = os.environ["DATABRICKS_CLIENT_SECRET"]
workspace_url = os.environ["DATABRICKS_WORKSPACE_URL"]
token_endpoint_url = f"{workspace_url}/oidc/v1/token"

# List of known neighborhoods (you should update this with your actual neighborhoods)
KNOWN_NEIGHBORHOODS = [
    'NAmes', 'Edwards', 'BrkSide', 'OldTown', 'SWISU', 'ClearCr', 'Crawfor', 
    'Mitchel', 'NoRidge', 'Timber', 'StoneBr', 'NWAmes', 'Gilbert', 'Somerst'
]

@app.route(route="PricePrediction", methods=["POST"])
def PricePrediction(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    try:
        req_body = req.get_json()
        print(f"Request body: {req_body}")
    except ValueError:
        return func.HttpResponse("Invalid JSON in request body.", status_code=400)

    # Extract required fields
    required_fields = [
        "Neighborhood", "YearBuilt", "TotalBsmtSF", "GrLivArea", 
        "OverallQual", "FullBath", "TotRmsAbvGrd", "GarageArea"
    ]
    numeric_fields = [
        "YearBuilt", "TotalBsmtSF", "GrLivArea", 
        "OverallQual", "FullBath", "TotRmsAbvGrd", "GarageArea"
    ]
    
    numeric_data = {}
    missing_fields = []

    # Validate and convert input fields
    for field in required_fields:
        value = req_body.get(field)
        if value is None:
            missing_fields.append(field)
            continue
            
        if field in numeric_fields:
            try:
                numeric_value = float(value)
                # Additional validation for specific fields
                if field in ["OverallQual", "FullBath", "TotRmsAbvGrd"] and not float(value).is_integer():
                    return func.HttpResponse(f"Field '{field}' must be an integer.", status_code=400)
                if field in ["TotalBsmtSF", "GrLivArea", "GarageArea"] and float(value) < 0:
                    return func.HttpResponse(f"Field '{field}' cannot be negative.", status_code=400)
                numeric_data[field] = numeric_value
            except (ValueError, TypeError):
                return func.HttpResponse(f"Field '{field}' must be a valid number.", status_code=400)

    if missing_fields:
        return func.HttpResponse(f"Missing required fields: {', '.join(missing_fields)}", status_code=400)

    try:
        # Create feature array for numeric fields
        numeric_features = []
        for field in numeric_fields:
            numeric_features.append(numeric_data[field])
        
        # Convert to numpy array and reshape for numeric features
        numeric_array = np.array(numeric_features, dtype=np.float64).reshape(1, -1)

        # Get the neighborhood
        neighborhood = req_body.get("Neighborhood")
        if neighborhood not in KNOWN_NEIGHBORHOODS:
            return func.HttpResponse(
                f"Invalid neighborhood. Must be one of: {', '.join(KNOWN_NEIGHBORHOODS)}",
                status_code=400
            )
        
        # Create DataFrame with all features
        all_feature_names = numeric_fields + ["Neighborhood"]
        
        # Create a list of all features including neighborhood
        all_features = np.concatenate([numeric_array, np.array([[neighborhood]])], axis=1)
        df = pd.DataFrame(all_features, columns=all_feature_names)

        # Create the payload
        payload = {
            "dataframe_split": {
                "columns": all_feature_names,
                "data": df.values.tolist()
            }
        }
        
        logging.info(f"Calling prediction service with payload: {json.dumps(payload)}")
        prediction = score_model(payload)
        
        return func.HttpResponse(
            body=json.dumps({"predicted_price": prediction}),
            status_code=200,
            mimetype="application/json"
        )
    except Exception as e:
        logging.error(f"Error calling MLflow endpoint: {str(e)}")
        return func.HttpResponse(
            body=json.dumps({"error": f"Error calling prediction service: {str(e)}"}),
            status_code=500,
            mimetype="application/json"
        )

def get_workspace_oauth_token():
    response = requests.post(
        token_endpoint_url,
        auth=(client_id, client_secret),
        data={
            "grant_type": "client_credentials",
            "scope": "all-apis"
        }
    )
    response.raise_for_status()
    return response.json().get("access_token")

def create_tf_serving_json(data):
    if isinstance(data, pd.DataFrame):
        return {
            "dataframe_split": {
                "columns": data.columns.tolist(),
                "data": data.values.tolist()
            }
        }
    elif isinstance(data, dict) and "dataframe_split" in data:
        return data
    return {'inputs': data.tolist() if hasattr(data, 'tolist') else data}

def score_model(dataset):
    access_token = get_workspace_oauth_token()
    headers = {'Authorization': f'Bearer {access_token}', 'Content-Type': 'application/json'}
    
    ds_dict = create_tf_serving_json(dataset)
    data_json = json.dumps(ds_dict, allow_nan=True)
    print(f"Requesting prediction with data: {data_json}")
    
    response = requests.request(method='POST', headers=headers, url=MLFLOW_ENDPOINT_URL, data=data_json)
    if response.status_code != 200:
        raise Exception(f'Request failed with status {response.status_code}, {response.text}')
    return response.json()