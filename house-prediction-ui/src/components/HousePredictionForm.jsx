import { useState } from "react";
import axios from "axios";
import "./HousePredictionForm.css";

const API_BASE_URL = "https://localhost:7238/api"; // Update this with your actual API base URL

const HousePredictionForm = () => {
  const [predictedPrice, setPredictedPrice] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [trainingStatus, setTrainingStatus] = useState(null);
  const [formData, setFormData] = useState({
    neighborhood: "",
    yearBuilt: "",
    totalBsmtSF: "",
    grLivArea: "",
    overallQual: "",
    fullBath: "",
    totRmsAbvGrd: "",
    garageArea: "",
    salePrice: "0",
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await axios.post(`${API_BASE_URL}/details`, formData);
      alert("Data submitted successfully!");
    } catch (err) {
      setError(err.response?.data?.message || "Error submitting data");
      console.error("Error:", err);
    } finally {
      setLoading(false);
    }
  };

  const handlePredict = async () => {
    setLoading(true);
    setPredictedPrice(null);
    setError(null);

    try {
      const response = await axios.post(`${API_BASE_URL}/predict`, formData);
    
      const predictedValue = response.data.predicted_price.predictions[0];
      setPredictedPrice(predictedValue);
      // Update the sale price field with the predicted value, default to 0 if null
      setFormData(prev => ({
        ...prev,
        salePrice: predictedValue?.toString() || "0"
      }));
    } catch (err) {
      if (err.response?.status === 429) {
        setError("Too many requests. Please wait a moment before trying again.");
      } else {
        setError(err.response?.data?.error || "Error predicting price.Try after sometime");
      }
      console.error("Error:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleTrainModel = async () => {
    setLoading(true);
    setTrainingStatus("Training started...");
    setError(null);

    try {
      const response = await axios.get(`${API_BASE_URL}/model/train`);
      setTrainingStatus(
        response.data.message || "Training completed successfully!"
      );
      // Clear training status after 5 seconds
      setTimeout(() => {
        setTrainingStatus(null);
      }, 5000);
    
    } catch (err) {
      if (err.response?.status === 429) {
        setError("Too many training requests. Please wait a moment before trying again.");
      } else {
        setError(err.response?.data?.error || "Error training model");
      }
      console.error("Error:", err);
      setTrainingStatus(null);
     
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-container">
      <h2>House Price Prediction</h2>
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="neighborhood">Neighborhood</label>
          <input
            type="text"
            id="neighborhood"
            name="neighborhood"
            value={formData.neighborhood}
            onChange={handleChange}
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="yearBuilt">Year Built</label>
          <input
            type="number"
            id="yearBuilt"
            name="yearBuilt"
            value={formData.yearBuilt}
            onChange={handleChange}
            min="1800"
            max={new Date().getFullYear()}
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="totalBsmtSF">Total Basement Square Feet</label>
          <input
            type="number"
            id="totalBsmtSF"
            name="totalBsmtSF"
            value={formData.totalBsmtSF}
            onChange={handleChange}
            min="0"
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="grLivArea">Above Ground Living Area</label>
          <input
            type="number"
            id="grLivArea"
            name="grLivArea"
            value={formData.grLivArea}
            onChange={handleChange}
            min="0"
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="overallQual">Overall Quality (1-10)</label>
          <input
            type="number"
            id="overallQual"
            name="overallQual"
            value={formData.overallQual}
            onChange={handleChange}
            min="1"
            max="10"
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="fullBath">Full Bathrooms</label>
          <input
            type="number"
            id="fullBath"
            name="fullBath"
            value={formData.fullBath}
            onChange={handleChange}
            min="0"
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="totRmsAbvGrd">Total Rooms Above Ground</label>
          <input
            type="number"
            id="totRmsAbvGrd"
            name="totRmsAbvGrd"
            value={formData.totRmsAbvGrd}
            onChange={handleChange}
            min="0"
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="garageArea">Garage Area</label>
          <input
            type="number"
            id="garageArea"
            name="garageArea"
            value={formData.garageArea}
            onChange={handleChange}
            min="0"
            required
          />
        </div>
        <div className="form-group">
          <label htmlFor="salePrice">Sale Price</label>
          <input
            type="number"
            id="salePrice"
            name="salePrice"
            value={formData.salePrice}
            onChange={handleChange}
            placeholder="Enter the sale price"
            min="0"
            step="1000"
            required
          />
        </div>
        <div className="button-group">
          <button
            type="button"
            className="predict-button"
            onClick={handlePredict}
            disabled={loading}
          >
            {loading ? "Processing..." : "Predict Price"}
          </button>
          <button type="submit" className="submit-button" disabled={loading}>
            {loading ? "Submitting..." : "Submit Data"}
          </button>
          <button
            type="button"
            className="train-button"
            onClick={handleTrainModel}
            disabled={loading}
          >
            {loading ? "Training..." : "Retrain Model"}
          </button>
        </div>

        {error && <div className="error-message">{error}</div>}

        {trainingStatus && (
          <div className="training-status">
            <h3>Training Status:</h3>
            <p>{trainingStatus}</p>
          </div>
        )}

        {predictedPrice && (
          <div className="prediction-result">
            <h3>Predicted House Price:</h3>
            <p>${predictedPrice.toLocaleString()}</p>
          </div>
        )}
      </form>
    </div>
  );
};

export default HousePredictionForm;
