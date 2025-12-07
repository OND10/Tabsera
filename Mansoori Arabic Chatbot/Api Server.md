"""

Flask API Server for Mansoori Arabic Chatbot

This creates a REST API endpoint that your .NET backend can call.

ENDPOINTS:

- POST /chat - Send question, get answer

- GET /health - Health check

USAGE:

1. Install: pip install flask flask-cors

2. Run: python api_server.py

3. Test: curl -X POST http://localhost:5000/chat -H "Content-Type: application/json" -d '{"question": "ماذا تعمل هذه المنصة؟"}'

"""

from flask import Flask, request, jsonify

from flask_cors import CORS

from transformers import AutoTokenizer, AutoModelForCausalLM

import torch

import os

from datetime import datetime

# ============================================

# CONFIGURATION

# ============================================

MODEL_PATH = "./mansoori-chatbot/final" # Path to trained model

PORT = 5000 # API port

HOST = '0.0.0.0' # Listen on all interfaces

# Generation parameters

MAX_LENGTH = 200 # Maximum answer length

TEMPERATURE = 0.7 # Randomness (0.0 = deterministic, 1.0 = creative)

TOP_P = 0.9 # Nucleus sampling

DO_SAMPLE = True # Use sampling (vs greedy)

# ============================================

# INITIALIZE FLASK APP

# ============================================

app = Flask(__name__)

CORS(app) # Enable CORS for cross-origin requests

# Global variables for model and tokenizer

model = None

tokenizer = None

device = None

# ============================================

# LOAD MODEL AT STARTUP

# ============================================

def load_model():

"""Load the trained model and tokenizer"""

global model, tokenizer, device

print("\n" + "="*70)

print("🤖 MANSOORI CHATBOT API SERVER")

print("="*70)

print(f"\n⏰ Starting at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

# Check if model exists

if not os.path.exists(MODEL_PATH):

print(f"\n❌ Error: Model not found at {MODEL_PATH}")

print(" Please train the model first using train_model.py")

exit(1)

print(f"\n📂 Loading model from: {MODEL_PATH}")

print(" This may take a minute...")

try:

# Load tokenizer

print("\n1️⃣ Loading tokenizer...")

tokenizer = AutoTokenizer.from_pretrained(MODEL_PATH)

print("✅ Tokenizer loaded")

# Load model

print("\n2️⃣ Loading model...")

model = AutoModelForCausalLM.from_pretrained(MODEL_PATH)

print("✅ Model loaded")

# Check for GPU

print("\n3️⃣ Checking GPU...")

if torch.cuda.is_available():

device = torch.device('cuda')

model = model.to(device)

gpu_name = torch.cuda.get_device_name(0)

print(f"✅ Using GPU: {gpu_name}")

else:

device = torch.device('cpu')

print("⚠️ No GPU found, using CPU (slower)")

# Set to evaluation mode

model.eval()

print("\n" + "="*70)

print("✅ MODEL LOADED SUCCESSFULLY!")

print("="*70)

print(f"\n🌐 API Server ready at: http://localhost:{PORT}")

print(f"\n📝 Endpoints:")

print(f" • POST /chat - Send questions")

print(f" • GET /health - Health check")

print("\n" + "="*70 + "\n")

except Exception as e:

print(f"\n❌ Error loading model: {e}")

exit(1)

# ============================================

# GENERATE ANSWER FUNCTION

# ============================================

def generate_answer(question):

"""

Generate answer for a given question

Args:

question (str): User's question in Arabic

Returns:

str: Generated answer

"""

try:

# Format the prompt

prompt = f"السؤال: {question}\nالجواب:"

# Tokenize

inputs = tokenizer(prompt, return_tensors="pt")

# Move to device (GPU or CPU)

if device:

inputs = {k: v.to(device) for k, v in inputs.items()}

# Generate answer

with torch.no_grad(): # Disable gradient calculation (faster inference)

outputs = model.generate(

inputs.input_ids,

max_length=MAX_LENGTH,

num_return_sequences=1,

temperature=TEMPERATURE,

top_p=TOP_P,

do_sample=DO_SAMPLE,

pad_token_id=tokenizer.eos_token_id,

eos_token_id=tokenizer.eos_token_id

)

# Decode the generated tokens

full_response = tokenizer.decode(outputs[0], skip_special_tokens=True)

# Extract just the answer part (after "الجواب:")

if "الجواب:" in full_response:

answer = full_response.split("الجواب:")[1].strip()

else:

answer = full_response.strip()

return answer

except Exception as e:

raise Exception(f"Error generating answer: {str(e)}")

# ============================================

# API ENDPOINTS

# ============================================

@app.route('/chat', methods=['POST'])

def chat():

"""

Chat endpoint - receives question and returns answer

Request body:

{

"question": "ماذا تعمل هذه المنصة؟"

}

Response:

{

"question": "ماذا تعمل هذه المنصة؟",

"answer": "منصة منصوري هي...",

"success": true,

"timestamp": "2024-01-01 12:00:00"

}

"""

try:

# Get JSON data from request

data = request.get_json()

# Validate request

if not data:

return jsonify({

"error": "No JSON data provided",

"success": False

}), 400

question = data.get('question', '').strip()

if not question:

return jsonify({

"error": "No question provided",

"success": False

}), 400

# Log the request

print(f"\n📥 Received question: {question}")

# Generate answer

answer = generate_answer(question)

# Log the response

print(f"📤 Generated answer: {answer[:100]}...")

# Return response

return jsonify({

"question": question,

"answer": answer,

"success": True,

"timestamp": datetime.now().strftime('%Y-%m-%d %H:%M:%S')

})

except Exception as e:

print(f"❌ Error: {str(e)}")

return jsonify({

"error": str(e),

"success": False

}), 500

@app.route('/health', methods=['GET'])

def health():

"""

Health check endpoint

Returns:

{

"status": "healthy",

"model_loaded": true,

"device": "cuda",

"timestamp": "2024-01-01 12:00:00"

}

"""

return jsonify({

"status": "healthy",

"model_loaded": model is not None,

"device": str(device) if device else "unknown",

"timestamp": datetime.now().strftime('%Y-%m-%d %H:%M:%S')

})

@app.route('/', methods=['GET'])

def index():

"""

Root endpoint - API information

"""

return jsonify({

"name": "Mansoori Arabic Chatbot API",

"version": "1.0",

"endpoints": {

"POST /chat": "Send question, get answer",

"GET /health": "Health check",

"GET /": "This page"

},

"example": {

"url": f"http://localhost:{PORT}/chat",

"method": "POST",

"body": {

"question": "ماذا تعمل هذه المنصة؟"

}

}

})

# ============================================

# ERROR HANDLERS

# ============================================

@app.errorhandler(404)

def not_found(error):

"""Handle 404 errors"""

return jsonify({

"error": "Endpoint not found",

"success": False

}), 404

@app.errorhandler(500)

def internal_error(error):

"""Handle 500 errors"""

return jsonify({

"error": "Internal server error",

"success": False

}), 500

# ============================================

# MAIN

# ============================================

if __name__ == '__main__':

# Load model before starting server

load_model()

# Start Flask server

print(f"🚀 Starting server on {HOST}:{PORT}...\n")

app.run(

host=HOST,

port=PORT,

debug=False # Set to True for development

)