import os
import re
import requests

API_KEY = os.getenv("OPENROUTER_API_KEY")
BASE_URL = "https://openrouter.ai/api/v1"

if not API_KEY:
    raise SystemExit("Missing OPENROUTER_API_KEY env var")

# Optional but recommended by OpenRouter for attribution
headers = {
    "Authorization": f"Bearer {API_KEY}",
    "Content-Type": "application/json",
    "HTTP-Referer": "https://localhost",  # replace with your app/site URL
    "X-Title": "light-copy-translator",   # replace with your app name
}

def pick_deepseek_chat_model():
    resp = requests.get(f"{BASE_URL}/models", headers=headers, timeout=15)
    resp.raise_for_status()
    models = resp.json().get("data", [])

    preferred = [
        "deepseek/deepseek-chat",
        "deepseek/deepseek-chat:free",
        "deepseek/deepseek-v3",
        "deepseek/deepseek-v3:free",
    ]
    available = {m.get("id") for m in models}
    for mid in preferred:
        if mid in available:
            return mid
    for m in models:
        mid = m.get("id", "")
        if mid.startswith("deepseek/") and "chat" in mid:
            return mid
    for m in models:
        mid = m.get("id", "")
        if mid.startswith("deepseek/"):
            return mid
    return None

def is_single_english_word(text):
    return bool(re.fullmatch(r"[A-Za-z][A-Za-z'-]*", text))

def build_messages(text):
    if is_single_english_word(text):
        sys_prompt = (
            "你是英译中词典。用户输入一个英文单词时，只输出中文释义。"
            "给出常见词性与简明释义，多词性分行，例如：\n"
            "n. 释义\nv. 释义\nadj. 释义\n"
            "不要给例句、不要解释、不要多余文本。"
        )
    else:
        sys_prompt = (
            "你是英译中翻译器。用户输入英文句子或段落时，"
            "只输出流畅准确的中文翻译，不要解释、不要附加内容。"
        )
    return [
        {"role": "system", "content": sys_prompt},
        {"role": "user", "content": text},
    ]

model_id = pick_deepseek_chat_model()
if not model_id:
    raise SystemExit("No DeepSeek chat model found in OpenRouter /models list")

user_text = input("请输入待翻译的英文内容：").strip()
if not user_text:
    raise SystemExit("输入为空")

payload = {
    "model": model_id,
    "temperature": 0.2,
    "messages": build_messages(user_text),
}

response = requests.post(f"{BASE_URL}/chat/completions", json=payload, headers=headers, timeout=30)

if response.status_code == 200:
    result = response.json()
    content = result["choices"][0]["message"]["content"]
    print(content)
else:
    print("Failed to fetch data from API.")
    print("Status Code:", response.status_code)
    print("Response:", response.text)
