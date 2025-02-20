# Set up
**動作確認済み**
* Windows 10
* Intel Core-i7 8700 / Core-i7 9700K
* NVIDIA GTX1080 / RTX2060
* CUDA 11.8
* cuDNN 8.6
* Python 3.8
* Oculus DK2 / Rift1
* Unity 2020.3.40f1

特にUnityはOculusを動かすため，CUDA/cuDNNはモデルデータを読み込むため，バージョンが違うと動かない可能性がある

## Install Oculus Software

以下からOculusソフトウェアをインストール  
https://www.oculus.com/Setup/  
起動後，「設定 > 一般 > 提供元不明」をオンにして許可する  
(自作アプリケーションを動かすため)

## Install Unity

以下からUnityまたはUnity Hubをインストール  
(2020.3.6以上 を入れること)  
https://unity3d.com/jp/get-unity/download


## Install Python

Pythonをインストール  
https://www.python.org/downloads/

## Install CUDA

以下からCUDAをインストール  
(v11.8で動作確認済み，バージョンによってはモデルを読み込めない可能性がある)  
https://developer.nvidia.com/cuda-toolkit-archive

## Install cuDNN

以下からcuDNNをインストール  
(v8.6で動作確認済み，CUDAとバージョンを合わせること)  
https://developer.nvidia.com/cuDNN

## Setup Source Codes

GitHubから本リポジトリをクローンorダウンロードして任意のディレクトリに設置  
(このときパスにマルチバイト文字があるとMediaPipeが起動しないので注意)

## Setup Python Environment

リポジトリのディレクトリから以下のコマンドを実行し，仮想環境を構築

    /> cd ./Backend/
    /Backend> python -m venv .venv
    /Backend> .venv/Script/activate
    (.venv) /Backend> pip install -r requirement.txt

## Setup Unity

Oculus Integrationをインポート  
https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022

NyARToolKit for Unityをダウンロード  
https://github.com/nyatla/NyARToolkitUnity/releases  
ダウンロードしたパッケージをUnityにて  
　Assets > Import package > Custom package　からインポート


