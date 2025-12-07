
"""

Data Preparation Script for Mansoori Arabic Chatbot

This script prepares the Q&A dataset for training

"""

import json

import pandas as pd

from sklearn.model_selection import train_test_split

print("="*60)

print("📊 MANSOORI CHATBOT - DATA PREPARATION")

print("="*60)

# ============================================

# STEP 1: Load Dataset

# ============================================

print("\n1️⃣ Loading dataset...")

try:

    with open('mansoori_qa_dataset.json', 'r', encoding='utf-8') as f:

        data = json.load(f)

    print(f"✅ Loaded {len(data)} Q&A pairs")

except FileNotFoundError:

    print("❌ Error: mansoori_qa_dataset.json not found!")

    print("   Please make sure the dataset file is in the same directory.")

    exit(1)

# ============================================

# STEP 2: Convert to Training Format

# ============================================

print("\n2️⃣ Converting to training format...")

training_data = []

for item in data:

    # Format: Question + Answer as one text

    # This is the format the model will learn

    text = f"السؤال: {item['question']}\nالجواب: {item['answer']}"

    training_data.append({"text": text})

print(f"✅ Converted {len(training_data)} examples")

# Show a sample

print("\n📝 Sample training example:")

print("-" * 60)

print(training_data[0]['text'])

print("-" * 60)

# ============================================

# STEP 3: Split into Train and Validation

# ============================================

print("\n3️⃣ Splitting into train and validation sets...")

# Convert to DataFrame for easier manipulation

df = pd.DataFrame(training_data)

# Split: 80% training, 20% validation

train_df, val_df = train_test_split(

    df, 

    test_size=0.2,      # 20% for validation

    random_state=42     # For reproducibility

)

print(f"✅ Training set: {len(train_df)} examples")

print(f"✅ Validation set: {len(val_df)} examples")

# ============================================

# STEP 4: Save to JSON Files

# ============================================

print("\n4️⃣ Saving to JSON files...")

# Save training data

train_df.to_json(

    'train.json', 

    orient='records',       # Each record is a JSON object

    force_ascii=False,      # Keep Arabic characters

    lines=True              # One JSON object per line (JSONL format)

)

print("✅ Saved train.json")

# Save validation data

val_df.to_json(

    'val.json', 

    orient='records', 

    force_ascii=False, 

    lines=True

)

print("✅ Saved val.json")

# ============================================

# STEP 5: Summary

# ============================================

print("\n" + "="*60)

print("🎉 DATA PREPARATION COMPLETE!")

print("="*60)

print(f"\n📊 Summary:")

print(f"   • Total examples: {len(data)}")

print(f"   • Training examples: {len(train_df)}")

print(f"   • Validation examples: {len(val_df)}")

print(f"\n📁 Output files:")

print(f"   • train.json")

print(f"   • val.json")

print(f"\n✅ Ready for training!")

print("="*60)