# UploadLargeFiles
This project implements a file upload service capable of handling large files efficiently by using chunk-based uploads. It supports the following features:
Chunked Uploads: Large files are split into smaller chunks and uploaded in parts to reduce memory usage and improve reliability.
Asynchronous Processing: The service processes file uploads asynchronously to enhance performance.
Progress Monitoring: Provides feedback during the upload process to indicate progress.
Backend Streaming: Combines received chunks into a single file or streams them to an API for further processing.
