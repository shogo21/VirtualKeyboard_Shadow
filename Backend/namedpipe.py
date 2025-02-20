import win32file

class NamedPipeClient():
    def __init__(self, pipe_name):
        self.pipe_name = pipe_name

    def connect(self):
        self.pipe = win32file.CreateFile(
            '\\\\.\\pipe\\'+self.pipe_name,
            win32file.GENERIC_READ | win32file.GENERIC_WRITE,
            0,
            None,
            win32file.OPEN_EXISTING,
            0,
            None
        )

    def read(self, byte_size: int) -> None:
        result, data = win32file.ReadFile(self.pipe, byte_size)
        if result == 0:
            return data
        else:
            return None

    def write(self, bytes_data: bytes) -> None:
        win32file.WriteFile(self.pipe, bytes_data)
