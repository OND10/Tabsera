
## 🚀 Quick Start (Copy-Paste Commands)

### 1. Setup Environment (Windows)

# Create virtual environment

python -m venv arabic_slm_env

# Activate it

arabic_slm_env\Scripts\activate

# Install dependencies

pip install -r requirements.txt

### 2. Prepare Data

python prepare_data.py

**Expected output**: `train.json` and `val.json` files created

### 3. Train Model (2-3 hours)

python train_model.py

**Expected output**: Model saved in `./mansoori-chatbot/final/`

### 4. Start API Server

python api_server.py

**Expected output**: Server running on `http://localhost:5000`

### 5. Test API

# Windows PowerShell

Invoke-RestMethod -Uri http://localhost:5000/chat -Method POST -ContentType "application/json" -Body '{"question": "ماذا تعمل هذه المنصة؟"}'

# Or use curl (if installed)

curl -X POST http://localhost:5000/chat -H "Content-Type: application/json" -d "{\"question\": \"ماذا تعمل هذه المنصة؟\"}"

---

## 📁 File Structure

arabic-slm-project/

├── mansoori_qa_dataset.json      # Original dataset (100 Q&A)

├── requirements.txt               # Python dependencies

├── prepare_data.py                # Data preparation script

├── train_model.py                 # Training script

├── api_server.py                  # API server

├── train.json                     # Training data (created by prepare_data.py)

├── val.json                       # Validation data (created by prepare_data.py)

└── mansoori-chatbot/              # Training outputs (created by train_model.py)

    ├── checkpoint-100/

    ├── checkpoint-200/

    └── final/                     # Final trained model (~540MB)

        ├── pytorch_model.bin

        ├── config.json

        └── tokenizer files

---

## 🔧 Common Issues & Solutions

### Issue 1: "CUDA out of memory"

**Solution 1**: Reduce batch size in 

train_model.py

BATCH_SIZE = 2  # Change from 4 to 2

**Solution 2**: Reduce max length

MAX_LENGTH = 256  # Change from 512 to 256

**Solution 3**: Use Google Colab (free GPU)

### Issue 2: "No module named 'transformers'"

**Solution**: Install requirements

pip install -r requirements.txt

### Issue 3: Training is very slow

**Cause**: No GPU detected

**Solution**: Use Google Colab

1. Go to [https://colab.research.google.com/](https://colab.research.google.com/)
2. Upload your files
3. Change runtime to GPU: Runtime → Change runtime type → GPU
4. Run the scripts in Colab

### Issue 4: Model gives bad answers

**Solution 1**: Train for more epochs

EPOCHS = 5  # Change from 3 to 5

**Solution 2**: Add more training data (augment dataset)

**Solution 3**: Adjust temperature (lower = more focused)

TEMPERATURE = 0.5  # Change from 0.7 to 0.5

---

## 🧪 Testing Commands

### Test with Python

from transformers import AutoTokenizer, AutoModelForCausalLM

# Load model

model = AutoModelForCausalLM.from_pretrained("./mansoori-chatbot/final")

tokenizer = AutoTokenizer.from_pretrained("./mansoori-chatbot/final")

# Ask question

question = "ماذا تعمل هذه المنصة؟"

prompt = f"السؤال: {question}\nالجواب:"

inputs = tokenizer(prompt, return_tensors="pt")

outputs = model.generate(inputs.input_ids, max_length=200)

answer = tokenizer.decode(outputs[0], skip_special_tokens=True)

print(answer)

### Test API with PowerShell

# Test chat endpoint

$body = @{

    question = "ماذا تعمل هذه المنصة؟"

} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:5000/chat -Method POST -ContentType "application/json" -Body $body

# Test health endpoint

Invoke-RestMethod -Uri http://localhost:5000/health -Method GET

---

## 🔌 .NET Integration

### C# Code to Call API

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

    [JsonPropertyName("question")]

    public string Question { get; set; }

    [JsonPropertyName("answer")]

    public string Answer { get; set; }

    [JsonPropertyName("success")]

    public bool Success { get; set; }

}

### Controller Example

[ApiController]

[Route("api/[controller]")]

public class ChatController : ControllerBase

{

    private readonly ChatbotService _chatbot;

    public ChatController(ChatbotService chatbot)

    {

        _chatbot = chatbot;

    }

    [HttpPost]

    public async Task<IActionResult> Chat([FromBody] ChatRequest request)

    {

        var answer = await _chatbot.GetAnswerAsync(request.Question);

        return Ok(new { answer });

    }

}

public class ChatRequest

{

    public string Question { get; set; }

}

---

## 📊 Model Information

|Property|Value|
|---|---|
|Model|AraGPT2-base (fine-tuned)|
|Parameters|135 million|
|Size|~540 MB|
|Language|Arabic|
|Task|Question Answering|
|Training Time|2-3 hours (RTX 3060)|
|Inference Speed|~50-100ms per response|

---

## ⚙️ Configuration Parameters

### Training Parameters

BATCH_SIZE = 4          # How many examples at once (2-8)

EPOCHS = 3              # How many times through data (2-5)

LEARNING_RATE = 5e-5    # How fast to learn (1e-5 to 1e-4)

MAX_LENGTH = 512        # Max tokens per example (256-1024)

### Generation Parameters

MAX_LENGTH = 200        # Max answer length (100-500)

TEMPERATURE = 0.7       # Randomness (0.1-1.0)

TOP_P = 0.9            # Nucleus sampling (0.8-0.95)

**Temperature Guide**:

- `0.1-0.3`: Very focused, deterministic
- `0.4-0.7`: Balanced (recommended)
- `0.8-1.0`: Creative, varied

---

## 📈 Performance Optimization

### Reduce Model Size

# Use 8-bit quantization (reduces size by 50%)

from transformers import AutoModelForCausalLM

model = AutoModelForCausalLM.from_pretrained(

    "./mansoori-chatbot/final",

    load_in_8bit=True,

    device_map="auto"

)

# Size: ~540MB → ~270MB

### Speed Up Inference

# Use GPU

model = model.to('cuda')

# Use half precision

model = model.half()

# Disable gradient calculation

with torch.no_grad():

    outputs = model.generate(...)

---

## 🐛 Debugging

### Check GPU

import torch

print(f"CUDA available: {torch.cuda.is_available()}")

print(f"GPU name: {torch.cuda.get_device_name(0)}")

print(f"GPU memory: {torch.cuda.get_device_properties(0).total_memory / 1e9:.1f} GB")

### Check Model Size

import os

model_path = "./mansoori-chatbot/final"

size_mb = sum(

    os.path.getsize(os.path.join(model_path, f)) 

    for f in os.listdir(model_path)

) / (1024 * 1024)

print(f"Model size: {size_mb:.0f} MB")

### Monitor Training

# In another terminal, monitor GPU usage

nvidia-smi -l 1  # Update every 1 second

---

## 🌐 Deployment

### Run API as Background Service (Windows)

# Install nssm (Non-Sucking Service Manager)

# Download from: https://nssm.cc/download

# Install as service

nssm install MansooriChatbot "C:\path\to\python.exe" "C:\path\to\api_server.py"

nssm start MansooriChatbot

### Run API with Docker

FROM python:3.10

WORKDIR /app

COPY requirements.txt .

RUN pip install -r requirements.txt

COPY . .

CMD ["python", "api_server.py"]

# Build

docker build -t mansoori-chatbot .

# Run

docker run -p 5000:5000 mansoori-chatbot

---

## 📞 Support

If you encounter issues:

1. Check the error message carefully
2. Look in the "Common Issues" section above
3. Verify all files are in the correct location
4. Ensure GPU drivers are up to date (if using GPU)
5. Try with a smaller model or batch size

---

## ✅ Checklist

Before training:

- [ ]  Python 3.10+ installed
- [ ]  GPU available (or Google Colab ready)
- [ ]  All files in same directory
- [ ]  
    
    requirements.txt installed
- [ ]  
    
    mansoori_qa_dataset.json present

After training:

- [ ]  Model saved in `./mansoori-chatbot/final/`
- [ ]  Model size ~540MB
- [ ]  Test questions work
- [ ]  API server runs
- [ ]  .NET integration tested