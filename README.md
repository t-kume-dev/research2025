# research2025: MR-based Item Management & LLM Paper Search

本リポジトリは、大学での研究成果の概要を管理するものです。現在、ソースコードの公開に向けて整理・クリーンアップを行っています。

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