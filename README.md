# Research: System for preventing forgetfulness before going out using mixed reality

本リポジトリは、HoloLens 2を用いた複合現実（MR）による外出支援システムの研究開発プロジェクトです。
現在、ソースコードの公開に向けて整理・クリーンアップを行っています。

## Project Overview
外出準備時におけるユーザーの認知的負荷（探し物や忘れ物への不安）を軽減することを目的とした支援システムです。

### Key Features
- Spatial Anchoring & Navigation: 現実世界の物体位置とデジタルツインを同期し、HoloLensを通じた直感的な位置提示を実現。
- Object Detection (Azure Custom Vision): 現実世界の特定物品をAIで識別し、システムと連動。
- Proprietary Grabbing Logic: 手首の座標を利用することで、オクルージョン（遮蔽）に強い非接触な把持判定アルゴリズムを独自実装。
- IoT Integration (SwitchBot API): 家電の施錠・消灯状態をMR空間内に統合し、一括確認を可能に。

### Evaluation
- 移動探索時間 / 忘れ物回数
- NASA-TLX / SUS
- 被験者実験を通じ、探索時間の短縮とワークロードの有意な低減を実証済み。

### Tech Stack
- Unity 2022 / C# (MRTK)
- Azure Custom Vision
- SwitchBot API