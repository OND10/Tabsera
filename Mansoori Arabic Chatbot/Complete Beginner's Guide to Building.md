
## 🎯 Goal

Build a small Arabic language model (~2GB max) in 1-2 days that can answer questions about the Mansoori platform, then integrate it with your .NET backend.

---

## 📚 Table of Contents

1. Understanding the Basics
2. Why You Need GPU/RAM/Storage
3. Required Tools & Libraries
4. Step-by-Step Implementation
5. Integration with .NET Backend
6. Fast Track (1-2 Days)

---

## 1. Understanding the Basics

### What is a Language Model?

Think of it like this:

- **Input**: User types "ماذا تعمل هذه المنصة؟"
- **Model**: Processes the text and predicts the best answer
- **Output**: "منصة منصوري هي منصة لإدارة الاشتراكات..."

### Key Concepts Explained Simply

#### **Tokenization** 🔤

**What it is**: Breaking text into smaller pieces (tokens) that the model can understand.

**Example**:

Text: "ماذا تعمل هذه المنصة؟"

Tokens: ["ماذا", "تعمل", "هذه", "المنصة", "؟"]

Token IDs: [1234, 5678, 9012, 3456, 7890]

**Why?**: Computers don't understand words, they understand numbers. Tokenization converts text → numbers.

#### **Training** 🏋️

**What it is**: Teaching the model to predict the right answer by showing it many examples.

**Process**:

1. Show model: Question → Answer
2. Model makes a prediction
3. Compare prediction to correct answer
4. Adjust model weights to improve
5. Repeat thousands of times

#### **Model Architecture** 🏗️

**What it is**: The structure/blueprint of your AI model.

**Simple analogy**:

- Architecture = Recipe for a cake
- Training = Actually baking the cake
- Dataset = Ingredients

---

## 2. Why Hardware Matters

### Why GPU? 🎮

**Simple explanation**:

- **CPU**: Can do 1-4 tasks at once (like having 1-4 workers)
- **GPU**: Can do 1000s of tasks simultaneously (like having 1000s of workers)

**For AI training**:

- Training involves millions of calculations
- GPU does them in parallel → **100x faster** than CPU
- Without GPU: Training takes weeks ❌
- With GPU: Training takes hours ✅

### Why 12GB VRAM?

**VRAM** = GPU memory (where the model lives during training)

**Size breakdown**:

- Model parameters: ~2-4GB
- Training data batches: ~2-3GB
- Gradients & optimizer: ~4-6GB
- **Total**: ~10-12GB needed

**What if you don't have GPU?** → Use **Google Colab** (free GPU in the cloud!)

### Why 16GB RAM?

- Loading dataset: ~2-4GB
- Python libraries: ~2-3GB
- Operating system: ~4-6GB
- Buffer: ~4-6GB

### Why 50GB Storage?

- Python + libraries: ~10GB
- Dataset: ~1GB
- Model checkpoints: ~10-20GB (saves progress every 500 steps)
- Final model: ~2GB
- Logs & cache: ~5-10GB

---

## 3. Required Tools & Libraries

### Python Libraries Explained

#### **Core Libraries**

# 1. transformers - The main library for language models

# What it does: Provides pre-built models and training tools

pip install transformers

# 2. torch (PyTorch) - Deep learning framework

# What it does: Handles all the math and GPU operations

pip install torch

# 3. datasets - For loading and processing data

# What it does: Makes it easy to work with training data

pip install datasets

# 4. accelerate - Makes training faster

# What it does: Optimizes GPU usage automatically

pip install accelerate

# 5. sentencepiece - For tokenization

# What it does: Breaks Arabic text into tokens

pip install sentencepiece

#### **Data Processing Libraries**

# pandas - For working with data tables (like Excel in Python)

pip install pandas

# numpy - For numerical operations

pip install numpy

# scikit-learn - For splitting data (train/test)

pip install scikit-learn

#### **Optional but Helpful**

# tqdm - Shows progress bars

pip install tqdm

# wandb - Tracks training progress (optional)

pip install wandb

### Why Each Library?

|Library|Purpose|Example|
|---|---|---|
|**transformers**|Main AI library|Load GPT-2, train models|
|**torch**|Math engine|GPU calculations|
|**datasets**|Data handling|Load JSON, split data|
|**pandas**|Data manipulation|Read CSV, filter data|
|**scikit-learn**|Data splitting|80% train, 20% test|
|**sentencepiece**|Tokenization|Text → Numbers|

---

## 4. Step-by-Step Implementation

### Phase 1: Setup Environment (30 minutes)

#### Option A: Local Setup (if you have GPU)

# 1. Install Python 3.10 or 3.11

# Download from: https://www.python.org/downloads/

# 2. Create virtual environment

python -m venv arabic_slm_env

# 3. Activate it

# Windows:

arabic_slm_env\Scripts\activate

# 4. Install libraries

pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118

pip install transformers datasets accelerate sentencepiece pandas scikit-learn tqdm

#### Option B: Google Colab (Recommended for beginners)

1. Go to: [https://colab.research.google.com/](https://colab.research.google.com/)
2. Click "New Notebook"
3. Change runtime to GPU:
    - Runtime → Change runtime type → GPU → Save
4. Install libraries in first cell:

!pip install transformers datasets accelerate sentencepiece

---

### Phase 2: Prepare Dataset (1 hour)

#### Step 1: Convert JSON to Training Format

Create file: `prepare_data.py`

import json

import pandas as pd

from sklearn.model_selection import train_test_split

# Load the dataset

with open('mansoori_qa_dataset.json', 'r', encoding='utf-8') as f:

    data = json.load(f)

# Convert to training format

training_data = []

for item in data:

    # Format: Question + Answer as one text

    text = f"السؤال: {item['question']}\nالجواب: {item['answer']}"

    training_data.append({"text": text})

# Convert to DataFrame

df = pd.DataFrame(training_data)

# Split into train (80%) and validation (20%)

train_df, val_df = train_test_split(df, test_size=0.2, random_state=42)

# Save to JSON files

train_df.to_json('train.json', orient='records', force_ascii=False, lines=True)

val_df.to_json('val.json', orient='records', force_ascii=False, lines=True)

print(f"Training samples: {len(train_df)}")

print(f"Validation samples: {len(val_df)}")

**What this does**:

1. Loads your 100 Q&A pairs
2. Formats them as "Question → Answer"
3. Splits into 80 training + 20 validation examples
4. Saves as JSON files

**Run it**:

python prepare_data.py

---

### Phase 3: Choose & Configure Model (30 minutes)

#### Understanding Model Size

For **~2GB max** size, here are your options:

|Model|Parameters|Size|Speed|Quality|
|---|---|---|---|---|
|**Tiny**|15M|~60MB|Very Fast|Basic|
|**Small**|50M|~200MB|Fast|Good|
|**Medium**|135M|~540MB|Medium|Very Good|
|**Large**|350M|~1.4GB|Slower|Excellent|

**Recommendation**: Use **135M parameters** (AraGPT2-base) for best balance.

#### Why Use Pre-trained Model?

**Option 1: Train from Scratch**

- Pros: Full control
- Cons: Needs 10,000+ examples, takes days
- ❌ Not suitable for 1-2 day timeline

**Option 2: Fine-tune Existing Model** ✅

- Pros: Needs only 100+ examples, takes hours
- Cons: Less control
- ✅ **Perfect for your case!**

---

### Phase 4: Training Script (Complete Code)

Create file: `train_model.py`

"""

Complete Training Script for Mansoori Arabic Chatbot

This script fine-tunes AraGPT2 on your Q&A dataset

"""

import torch

from transformers import (

    AutoTokenizer,

    AutoModelForCausalLM,

    TrainingArguments,

    Trainer,

    DataCollatorForLanguageModeling

)

from datasets import load_dataset

import os

# ============================================

# STEP 1: Configuration

# ============================================

print("🚀 Starting training setup...")

# Model configuration

MODEL_NAME = "aubmindlab/aragpt2-base"  # Pre-trained Arabic GPT-2

OUTPUT_DIR = "./mansoori-chatbot"       # Where to save trained model

MAX_LENGTH = 512                        # Maximum text length

# Training configuration

BATCH_SIZE = 4          # How many examples to process at once

EPOCHS = 3              # How many times to go through all data

LEARNING_RATE = 5e-5    # How fast the model learns (smaller = more careful)

SAVE_STEPS = 100        # Save checkpoint every 100 steps

# ============================================

# STEP 2: Load Tokenizer

# ============================================

print("📝 Loading tokenizer...")

tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)

tokenizer.pad_token = tokenizer.eos_token  # Set padding token

# ============================================

# STEP 3: Load Dataset

# ============================================

print("📊 Loading dataset...")

dataset = load_dataset('json', data_files={

    'train': 'train.json',

    'validation': 'val.json'

})

print(f"✅ Loaded {len(dataset['train'])} training examples")

print(f"✅ Loaded {len(dataset['validation'])} validation examples")

# ============================================

# STEP 4: Tokenize Dataset

# ============================================

print("🔤 Tokenizing dataset...")

def tokenize_function(examples):

    """

    Convert text to numbers (tokens) that the model understands

    """

    return tokenizer(

        examples["text"],

        truncation=True,        # Cut text if too long

        max_length=MAX_LENGTH,  # Maximum length

        padding="max_length",   # Pad shorter texts

        return_tensors=None

    )

# Apply tokenization to all examples

tokenized_dataset = dataset.map(

    tokenize_function,

    batched=True,           # Process multiple examples at once

    remove_columns=["text"] # Remove original text, keep only tokens

)

print("✅ Tokenization complete!")

# ============================================

# STEP 5: Load Model

# ============================================

print("🤖 Loading model...")

model = AutoModelForCausalLM.from_pretrained(MODEL_NAME)

# Count parameters

total_params = sum(p.numel() for p in model.parameters())

print(f"✅ Model loaded with {total_params:,} parameters")

print(f"📦 Estimated size: {total_params * 4 / 1e9:.2f} GB")

# ============================================

# STEP 6: Setup Training

# ============================================

print("⚙️ Setting up training...")

training_args = TrainingArguments(

    output_dir=OUTPUT_DIR,

    # Training parameters

    num_train_epochs=EPOCHS,

    per_device_train_batch_size=BATCH_SIZE,

    per_device_eval_batch_size=BATCH_SIZE,

    # Learning rate

    learning_rate=LEARNING_RATE,

    warmup_steps=100,  # Gradually increase learning rate

    # Evaluation

    eval_strategy="steps",

    eval_steps=100,

    # Saving

    save_steps=SAVE_STEPS,

    save_total_limit=2,  # Keep only 2 latest checkpoints

    # Logging

    logging_steps=50,

    logging_dir=f"{OUTPUT_DIR}/logs",

    # Performance

    fp16=True,  # Use mixed precision (faster training)

    # Other

    load_best_model_at_end=True,

    metric_for_best_model="loss",

    report_to="none"  # Don't use wandb/tensorboard

)

# Data collator (prepares batches)

data_collator = DataCollatorForLanguageModeling(

    tokenizer=tokenizer,

    mlm=False  # We're doing causal LM, not masked LM

)

# ============================================

# STEP 7: Create Trainer

# ============================================

print("🏋️ Creating trainer...")

trainer = Trainer(

    model=model,

    args=training_args,

    train_dataset=tokenized_dataset["train"],

    eval_dataset=tokenized_dataset["validation"],

    data_collator=data_collator,

)

# ============================================

# STEP 8: Train!

# ============================================

print("\n" + "="*50)

print("🎯 STARTING TRAINING!")

print("="*50 + "\n")

trainer.train()

print("\n" + "="*50)

print("✅ TRAINING COMPLETE!")

print("="*50 + "\n")

# ============================================

# STEP 9: Save Final Model

# ============================================

print("💾 Saving final model...")

model.save_pretrained(f"{OUTPUT_DIR}/final")

tokenizer.save_pretrained(f"{OUTPUT_DIR}/final")

print(f"✅ Model saved to: {OUTPUT_DIR}/final")

# ============================================

# STEP 10: Test the Model

# ============================================

print("\n" + "="*50)

print("🧪 TESTING MODEL")

print("="*50 + "\n")

# Load the trained model

trained_model = AutoModelForCausalLM.from_pretrained(f"{OUTPUT_DIR}/final")

trained_tokenizer = AutoTokenizer.from_pretrained(f"{OUTPUT_DIR}/final")

# Test questions

test_questions = [

    "ماذا تعمل هذه المنصة؟",

    "كيف أقوم بالاشتراك؟",

    "ما هي طرق الدفع المتاحة؟"

]

for question in test_questions:

    # Format question

    prompt = f"السؤال: {question}\nالجواب:"

    # Tokenize

    inputs = trained_tokenizer(prompt, return_tensors="pt")

    # Generate answer

    outputs = trained_model.generate(

        inputs.input_ids,

        max_length=200,

        num_return_sequences=1,

        temperature=0.7,

        top_p=0.9,

        do_sample=True,

        pad_token_id=trained_tokenizer.eos_token_id

    )

    # Decode answer

    answer = trained_tokenizer.decode(outputs[0], skip_special_tokens=True)

    print(f"❓ {question}")

    print(f"💬 {answer}\n")

print("="*50)

print("🎉 ALL DONE!")

print("="*50)

**What this script does (explained simply)**:

1. **Loads pre-trained Arabic model** (AraGPT2)
2. **Loads your Q&A data** (100 examples)
3. **Converts text to numbers** (tokenization)
4. **Trains the model** (teaches it your data)
5. **Saves the trained model** (for later use)
6. **Tests it** (asks sample questions)

**Run it**:

python train_model.py

---

### Phase 5: Understanding Training Configuration

Let me explain each parameter in simple terms:

BATCH_SIZE = 4

**What it means**: Process 4 examples at once **Why**: Larger = faster but needs more memory **Your case**: 4 is safe for 12GB GPU

EPOCHS = 3

**What it means**: Go through all data 3 times **Why**: More epochs = better learning (but can overfit) **Your case**: 3 is good for 100 examples

LEARNING_RATE = 5e-5

**What it means**: How big the learning steps are **Analogy**:

- Too large (1e-3): Like running downhill → might miss the target
- Too small (1e-7): Like walking slowly → takes forever
- Just right (5e-5): Like jogging → efficient **Your case**: 5e-5 is standard for fine-tuning

MAX_LENGTH = 512

**What it means**: Maximum tokens per example **Why**: Longer = more context but slower **Your case**: 512 is enough for Q&A

fp16=True

**What it means**: Use 16-bit numbers instead of 32-bit **Why**: 2x faster, uses half the memory **Your case**: Always use this!

---

## 5. Integration with .NET Backend

### Step 1: Create Python API Server

Create file: `api_server.py`

"""

Simple Flask API server for the chatbot

"""

from flask import Flask, request, jsonify

from transformers import AutoTokenizer, AutoModelForCausalLM

import torch

app = Flask(__name__)

# Load model once at startup

print("Loading model...")

model = AutoModelForCausalLM.from_pretrained("./mansoori-chatbot/final")

tokenizer = AutoTokenizer.from_pretrained("./mansoori-chatbot/final")

print("Model loaded!")

@app.route('/chat', methods=['POST'])

def chat():

    """

    Endpoint: POST /chat

    Body: {"question": "ماذا تعمل هذه المنصة؟"}

    Response: {"answer": "منصة منصوري هي..."}

    """

    try:

        # Get question from request

        data = request.get_json()

        question = data.get('question', '')

        if not question:

            return jsonify({"error": "No question provided"}), 400

        # Format prompt

        prompt = f"السؤال: {question}\nالجواب:"

        # Tokenize

        inputs = tokenizer(prompt, return_tensors="pt")

        # Generate answer

        outputs = model.generate(

            inputs.input_ids,

            max_length=200,

            temperature=0.7,

            top_p=0.9,

            do_sample=True,

            pad_token_id=tokenizer.eos_token_id

        )

        # Decode

        full_response = tokenizer.decode(outputs[0], skip_special_tokens=True)

        # Extract just the answer part

        answer = full_response.split("الجواب:")[-1].strip()

        return jsonify({

            "question": question,

            "answer": answer,

            "success": True

        })

    except Exception as e:

        return jsonify({

            "error": str(e),

            "success": False

        }), 500

@app.route('/health', methods=['GET'])

def health():

    """Health check endpoint"""

    return jsonify({"status": "healthy"})

if __name__ == '__main__':

    app.run(host='0.0.0.0', port=5000)

**Install Flask**:

pip install flask flask-cors

**Run the API**:

python api_server.py

### Step 2: Call from .NET

In your .NET backend:

using System.Net.Http;

using System.Text;

using System.Text.Json;

public class ChatbotService

{

    private readonly HttpClient _httpClient;

    private const string API_URL = "http://localhost:5000/chat";

    public ChatbotService()

    {

        _httpClient = new HttpClient();

    }

    public async Task<string> GetAnswerAsync(string question)

    {

        var requestBody = new { question = question };

        var json = JsonSerializer.Serialize(requestBody);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(API_URL, content);

        var responseJson = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ChatResponse>(responseJson);

        return result?.Answer ?? "عذراً، حدث خطأ";

    }

}

public class ChatResponse

{

    public string Question { get; set; }

    public string Answer { get; set; }

    public bool Success { get; set; }

}

---

## 6. Fast Track Plan (1-2 Days)

### Day 1: Setup & Training (6-8 hours)

**Morning (3-4 hours)**:

- ✅ Setup Google Colab (30 min)
- ✅ Install libraries (30 min)
- ✅ Prepare dataset (1 hour)
- ✅ Understand the code (1-2 hours)

**Afternoon (3-4 hours)**:

- ✅ Run training script (2-3 hours)
- ✅ Test the model (30 min)
- ✅ Fix any issues (30 min)

### Day 2: API & Integration (4-6 hours)

**Morning (2-3 hours)**:

- ✅ Create Flask API (1 hour)
- ✅ Test API locally (30 min)
- ✅ Deploy API (30 min - 1 hour)

**Afternoon (2-3 hours)**:

- ✅ Integrate with .NET (1-2 hours)
- ✅ Test end-to-end (1 hour)

---

## 7. Troubleshooting Common Issues

### Issue 1: Out of Memory (OOM)

**Error**: `CUDA out of memory`

**Solutions**:

# Reduce batch size

BATCH_SIZE = 2  # Instead of 4

# Reduce max length

MAX_LENGTH = 256  # Instead of 512

### Issue 2: Slow Training

**Solutions**:

- Use Google Colab Pro ($10/month) for faster GPU
- Reduce epochs to 2
- Use smaller model (50M params)

### Issue 3: Model Gives Bad Answers

**Solutions**:

- Increase epochs to 5
- Add more training examples
- Adjust temperature (lower = more focused)

---

## 8. Quick Start Commands

# 1. Setup

python -m venv env

env\Scripts\activate

pip install transformers datasets accelerate torch pandas scikit-learn flask

# 2. Prepare data

python prepare_data.py

# 3. Train model (2-3 hours)

python train_model.py

# 4. Start API

python api_server.py

# 5. Test

curl -X POST http://localhost:5000/chat -H "Content-Type: application/json" -d "{\"question\": \"ماذا تعمل هذه المنصة؟\"}"

---

## 🎉 You're Ready!

You now have everything you need to build your Arabic chatbot in 1-2 days. The model will be ~540MB (well under your 2GB limit), and you can integrate it with your .NET backend via a simple API.

**Good luck! 🚀**

