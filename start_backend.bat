set backend_path=.\Backend

pushd %backend_path%
.\.venv\Scripts\python.exe main.py

popd
copy .\logs.csv %backend_path%\logs\front_logs.csv
del .\logs.csv

pushd %backend_path%
.\.venv\Scripts\python.exe merge_logs.py