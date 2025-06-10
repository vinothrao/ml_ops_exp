import { useState } from 'react'
import './App.css'
import HousePredictionForm from './components/HousePredictionForm'

function App() {
  return (
    <div className="app-container">
      <header className="app-header">
        <h1>House Price Predictor</h1>
      </header>
      <main>
        <HousePredictionForm />
      </main>
    </div>
  )
}

export default App
