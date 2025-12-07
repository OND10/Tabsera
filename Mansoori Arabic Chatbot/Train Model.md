

"""

Training Script for Mansoori Arabic Chatbot

This script fine-tunes AraGPT2 on your Q&A dataset

REQUIREMENTS:

- Python 3.10+

- GPU with 12GB+ VRAM (or use Google Colab)

- Files: train.json, val.json (from prepare_data.py)

ESTIMATED TIME: 2-3 hours on RTX 3060

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

from datetime import datetime

# ============================================

# CONFIGURATION

# ============================================

# Model settings

MODEL_NAME = "aubmindlab/aragpt2-base" # Pre-trained Arabic GPT-2 (135M params)

OUTPUT_DIR = "./mansoori-chatbot" # Where to save the trained model

MAX_LENGTH = 512 # Maximum text length in tokens

# Training settings

BATCH_SIZE = 4 # Process 4 examples at once (reduce to 2 if OOM)

EPOCHS = 3 # Go through all data 3 times

LEARNING_RATE = 5e-5 # Learning rate (standard for fine-tuning)

SAVE_STEPS = 100 # Save checkpoint every 100 steps

WARMUP_STEPS = 100 # Gradually increase learning rate for first 100 steps

# ============================================

# WELCOME MESSAGE

# ============================================

print("\n" + "="*70)

print("🤖 MANSOORI ARABIC CHATBOT - TRAINING SCRIPT")

print("="*70)

print(f"\n⏰ Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

print(f"\n📋 Configuration:")

print(f" • Model: {MODEL_NAME}")

print(f" • Batch size: {BATCH_SIZE}")

print(f" • Epochs: {EPOCHS}")

print(f" • Learning rate: {LEARNING_RATE}")

print(f" • Max length: {MAX_LENGTH} tokens")

print("="*70)

# ============================================

# STEP 1: Check GPU

# ============================================

print("\n1️⃣ Checking GPU availability...")

if torch.cuda.is_available():

gpu_name = torch.cuda.get_device_name(0)

gpu_memory = torch.cuda.get_device_properties(0).total_memory / 1e9

print(f"✅ GPU found: {gpu_name}")

print(f" Memory: {gpu_memory:.1f} GB")

else:

print("⚠️ No GPU found! Training will be VERY slow on CPU.")

print(" Consider using Google Colab with GPU runtime.")

response = input(" Continue anyway? (y/n): ")

if response.lower() != 'y':

exit(0)

# ============================================

# STEP 2: Load Tokenizer

# ============================================

print("\n2️⃣ Loading tokenizer...")

try:

tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)

tokenizer.pad_token = tokenizer.eos_token # Set padding token

print(f"✅ Tokenizer loaded")

print(f" Vocabulary size: {len(tokenizer):,} tokens")

except Exception as e:

print(f"❌ Error loading tokenizer: {e}")

exit(1)

# ============================================

# STEP 3: Load Dataset

# ============================================

print("\n3️⃣ Loading dataset...")

try:

dataset = load_dataset('json', data_files={

'train': 'train.json',

'validation': 'val.json'

})

train_size = len(dataset['train'])

val_size = len(dataset['validation'])

print(f"✅ Dataset loaded")

print(f" Training examples: {train_size}")

print(f" Validation examples: {val_size}")

# Show a sample

print(f"\n📝 Sample training example:")

print("-" * 70)

print(dataset['train'][0]['text'][:200] + "...")

print("-" * 70)

except FileNotFoundError:

print("❌ Error: train.json or val.json not found!")

print(" Please run prepare_data.py first.")

exit(1)

except Exception as e:

print(f"❌ Error loading dataset: {e}")

exit(1)

# ============================================

# STEP 4: Tokenize Dataset

# ============================================

print("\n4️⃣ Tokenizing dataset...")

print(" This may take a few minutes...")

def tokenize_function(examples):

"""

Convert text to numbers (tokens) that the model understands

What happens here:

1. Take the text (Arabic Q&A)

2. Convert to token IDs (numbers)

3. Truncate if too long

4. Pad if too short

"""

return tokenizer(

examples["text"],

truncation=True, # Cut text if longer than MAX_LENGTH

max_length=MAX_LENGTH, # Maximum length

padding="max_length", # Pad shorter texts to MAX_LENGTH

return_tensors=None

)

# Apply tokenization to all examples

tokenized_dataset = dataset.map(

tokenize_function,

batched=True, # Process multiple examples at once (faster)

remove_columns=["text"] # Remove original text, keep only tokens

)

print("✅ Tokenization complete!")

# ============================================

# STEP 5: Load Model

# ============================================

print("\n5️⃣ Loading pre-trained model...")

print(" This may take a few minutes to download...")

try:

model = AutoModelForCausalLM.from_pretrained(MODEL_NAME)

# Count parameters

total_params = sum(p.numel() for p in model.parameters())

trainable_params = sum(p.numel() for p in model.parameters() if p.requires_grad)

print(f"✅ Model loaded")

print(f" Total parameters: {total_params:,}")

print(f" Trainable parameters: {trainable_params:,}")

print(f" Model size: ~{total_params * 4 / 1e9:.2f} GB")

except Exception as e:

print(f"❌ Error loading model: {e}")

exit(1)

# ============================================

# STEP 6: Setup Training

# ============================================

print("\n6️⃣ Setting up training configuration...")

training_args = TrainingArguments(

# Output

output_dir=OUTPUT_DIR,

# Training parameters

num_train_epochs=EPOCHS,

per_device_train_batch_size=BATCH_SIZE,

per_device_eval_batch_size=BATCH_SIZE,

# Learning rate

learning_rate=LEARNING_RATE,

warmup_steps=WARMUP_STEPS,

# Evaluation

eval_strategy="steps", # Evaluate every N steps

eval_steps=100, # Evaluate every 100 steps

# Saving

save_steps=SAVE_STEPS,

save_total_limit=2, # Keep only 2 latest checkpoints

# Logging

logging_steps=50,

logging_dir=f"{OUTPUT_DIR}/logs",

# Performance optimizations

fp16=torch.cuda.is_available(), # Use mixed precision if GPU available

# Other

load_best_model_at_end=True,

metric_for_best_model="loss",

greater_is_better=False,

report_to="none" # Don't use wandb/tensorboard

)

# Data collator (prepares batches for training)

data_collator = DataCollatorForLanguageModeling(

tokenizer=tokenizer,

mlm=False # We're doing causal language modeling, not masked LM

)

print("✅ Training configuration ready")

# ============================================

# STEP 7: Create Trainer

# ============================================

print("\n7️⃣ Creating trainer...")

trainer = Trainer(

model=model,

args=training_args,

train_dataset=tokenized_dataset["train"],

eval_dataset=tokenized_dataset["validation"],

data_collator=data_collator,

)

print("✅ Trainer created")

# Calculate estimated time

steps_per_epoch = len(dataset['train']) // BATCH_SIZE

total_steps = steps_per_epoch * EPOCHS

estimated_minutes = total_steps * 0.5 # Rough estimate: 30 seconds per step

print(f"\n📊 Training plan:")

print(f" • Steps per epoch: {steps_per_epoch}")

print(f" • Total steps: {total_steps}")

print(f" • Estimated time: {estimated_minutes:.0f} minutes (~{estimated_minutes/60:.1f} hours)")

# ============================================

# STEP 8: Train!

# ============================================

print("\n" + "="*70)

print("🎯 STARTING TRAINING!")

print("="*70)

print("\n⏳ This will take 2-3 hours. You can:")

print(" • Monitor progress below")

print(" • Check GPU usage with: nvidia-smi")

print(" • Take a break ☕")

print("\n" + "="*70 + "\n")

try:

# Start training

train_result = trainer.train()

print("\n" + "="*70)

print("✅ TRAINING COMPLETE!")

print("="*70)

print(f"\n📊 Training results:")

print(f" • Final loss: {train_result.training_loss:.4f}")

print(f" • Total time: {train_result.metrics['train_runtime']:.0f} seconds")

print(f" • Samples per second: {train_result.metrics['train_samples_per_second']:.2f}")

except KeyboardInterrupt:

print("\n⚠️ Training interrupted by user!")

print(" Partial model has been saved in checkpoints.")

exit(0)

except Exception as e:

print(f"\n❌ Error during training: {e}")

exit(1)

# ============================================

# STEP 9: Save Final Model

# ============================================

print("\n8️⃣ Saving final model...")

final_model_path = f"{OUTPUT_DIR}/final"

model.save_pretrained(final_model_path)

tokenizer.save_pretrained(final_model_path)

print(f"✅ Model saved to: {final_model_path}")

# Get model size

model_size = sum(

os.path.getsize(os.path.join(final_model_path, f))

for f in os.listdir(final_model_path)

if os.path.isfile(os.path.join(final_model_path, f))

) / (1024 * 1024) # Convert to MB

print(f" Model size: {model_size:.0f} MB (~{model_size/1024:.2f} GB)")

# ============================================

# STEP 10: Test the Model

# ============================================

print("\n9️⃣ Testing the trained model...")

print("-" * 70)

# Load the trained model

trained_model = AutoModelForCausalLM.from_pretrained(final_model_path)

trained_tokenizer = AutoTokenizer.from_pretrained(final_model_path)

# Move to GPU if available

if torch.cuda.is_available():

trained_model = trained_model.to('cuda')

# Test questions

test_questions = [

"ماذا تعمل هذه المنصة؟",

"كيف أقوم بالاشتراك؟",

"ما هي طرق الدفع المتاحة؟",

"هل التطبيق متاح على أجهزة آبل؟",

"كيف أتواصل مع الدعم الفني؟"

]

print("\n🧪 Testing with sample questions:\n")

for i, question in enumerate(test_questions, 1):

# Format question

prompt = f"السؤال: {question}\nالجواب:"

# Tokenize

inputs = trained_tokenizer(prompt, return_tensors="pt")

if torch.cuda.is_available():

inputs = {k: v.to('cuda') for k, v in inputs.items()}

# Generate answer

with torch.no_grad():

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

full_response = trained_tokenizer.decode(outputs[0], skip_special_tokens=True)

# Extract just the answer part

if "الجواب:" in full_response:

answer = full_response.split("الجواب:")[1].strip()

else:

answer = full_response

print(f"{i}. ❓ {question}")

print(f" 💬 {answer}\n")

print("-" * 70)

# ============================================

# FINAL SUMMARY

# ============================================

print("\n" + "="*70)

print("🎉 ALL DONE!")

print("="*70)

print(f"\n✅ Your Arabic chatbot is ready!")

print(f"\n📁 Model location: {final_model_path}")

print(f"📦 Model size: {model_size:.0f} MB")

print(f"\n🚀 Next steps:")

print(f" 1. Test more questions to verify quality")

print(f" 2. Run api_server.py to create API endpoint")

print(f" 3. Integrate with your .NET backend")

print(f"\n⏰ Finished at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

print("="*70 + "\n")