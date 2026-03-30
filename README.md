# research2025: MR-based Item Management & LLM Paper Search

本リポジトリは、大学での研究成果および研究室内開発プロジェクトの概要を管理するものです。現在、ソースコードの公開に向けて整理・クリーンアップを行っています。

## 1. MR-based Outing Support System (HoloLens 2)
外出準備時の心理的・認知的負荷を軽減するためのMRアプリケーションです。

### Key Features
- **Spatial Anchoring:** 現実世界の物体位置とデジタルツインを同期。
- **Object Detection:** **Azure Custom Vision** を用いた特定物品のリアルタイム識別。
- **Hand Tracking Logic:** MRTKを活用し、手首の座標を利用したオクルージョンに強い「把持判定アルゴリズム」を自作。
- **IoT Integration:** **SwitchBot API** を通じた家電（施錠・消灯）の状態確認・制御。

### Tech Stack
- Unity 2021.x / C#
- MRTK (Mixed Reality Toolkit)
- Azure Custom Vision
- SwitchBot API

---

## 2. LLM-based Research Paper Search & Chat (RAG)
研究室内の過去論文を効率的に探索・要約するためのシステムです。

### Key Features
- **Semantic Search:** **Gemini API** (Embedding) を用いた、意味内容に基づく論文検索。
- **RAG (Retrieval-Augmented Generation):** 抽出した論文コンテキストに基づき、Geminiが内容の要約や質疑応答に対応。
- **PDF Processing:** Pythonを用いた非構造化データ（PDF）のテキスト構造化。

### Tech Stack
- Python
- Gemini API (Google AI SDK)
- Vector DB (Chroma 等)
- Flask / FastAPI