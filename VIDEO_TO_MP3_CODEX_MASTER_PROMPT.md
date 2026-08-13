# MASTER PROMPT CHO CODEX
## Dự án: Video To MP3 Desktop App

> Đây là FILE ĐIỀU PHỐI DUY NHẤT của dự án.
> Codex phải đọc toàn bộ file này trước khi bắt đầu.
> Sau lần đầu, khi người dùng chỉ nói: `tiếp`, `bước tiếp theo`, `prompt tiếp theo`, `continue` hoặc câu tương đương, Codex phải tự mở lại file này, xác định bước chưa hoàn thành đầu tiên và thực hiện đúng bước đó.
> Không yêu cầu người dùng dán lại prompt.
> Không tự ý nhảy bước.
> Không tạo thêm file prompt/roadmap/checklist/AGENTS khác để điều phối dự án. Trạng thái kế hoạch phải được cập nhật trực tiếp trong chính file này.

---

# 0. MỤC TIÊU DỰ ÁN

Xây dựng một ứng dụng desktop Windows tên tạm thời:

**Video To MP3**

Ứng dụng cho phép người dùng:

1. Chọn một hoặc nhiều file video từ máy tính.
2. Kéo thả một hoặc nhiều file video vào cửa sổ.
3. Dán một hoặc nhiều URL video, mỗi URL một dòng.
4. Trộn file local và URL trong cùng một danh sách xử lý.
5. Chuyển đổi toàn bộ sang MP3.
6. Theo dõi tiến trình từng job và tiến trình tổng.
7. Hủy job, retry job lỗi, xóa job khỏi queue.
8. Chọn chất lượng MP3.
9. Chọn thư mục đầu ra.
10. Mở file MP3 hoặc mở thư mục chứa file sau khi hoàn thành.
11. Lưu Settings và History.
12. Hỗ trợ metadata/thumbnail ở các giai đoạn sau.
13. Hỗ trợ playlist URL ở các giai đoạn sau.
14. Đóng gói thành ứng dụng Windows có thể phát hành.

Ứng dụng phải ưu tiên:

- Dễ sử dụng.
- UI sạch, hiện đại, gần với giao diện mẫu ban đầu.
- Hoạt động ổn định với danh sách nhiều file/URL.
- Không khóa UI trong lúc download/convert.
- Có cancellation đúng nghĩa.
- Báo lỗi rõ ràng.
- Kiến trúc dễ bảo trì và mở rộng.
- Không viết lại chức năng encode audio nếu FFmpeg đã làm tốt.

---

# 1. QUYẾT ĐỊNH CÔNG NGHỆ

## 1.1 Ngôn ngữ

Ưu tiên sử dụng:

**C# + .NET Desktop**

Không dùng C++ trừ khi có lý do kỹ thuật thực sự bắt buộc và phải giải thích cho người dùng trước khi đổi.

Lý do chọn C#:

- Phù hợp Windows desktop.
- Dễ làm UI.
- Quản lý process FFmpeg/yt-dlp thuận tiện.
- Async/await, cancellation và progress tốt.
- Dễ đóng gói.
- Dễ bảo trì hơn C++ đối với loại ứng dụng này.

## 1.2 UI framework

Ưu tiên theo thứ tự:

1. WPF trên .NET hiện đại, nếu mục tiêu là độ ổn định và phát triển nhanh.
2. WinUI 3 chỉ chọn nếu môi trường dự án đã sẵn sàng và không làm tăng độ phức tạp không cần thiết.

Mặc định: **WPF**.

Áp dụng pattern:

**MVVM**

Không nhồi toàn bộ logic vào `MainWindow.xaml.cs`.

## 1.3 Công cụ xử lý media

Sử dụng:

- `FFmpeg`
- `ffprobe`
- `yt-dlp`

Nguyên tắc:

- File local: xử lý trực tiếp bằng FFmpeg/ffprobe.
- URL online: dùng yt-dlp để lấy thông tin và tải media/audio hợp lệ, sau đó dùng FFmpeg khi cần.
- Không triển khai cơ chế vượt DRM.
- Không cố phá cơ chế bảo vệ nội dung của nền tảng.
- Chỉ hỗ trợ việc tải/chuyển đổi nội dung mà người dùng có quyền truy cập/tải.

## 1.4 Lưu dữ liệu

Sử dụng SQLite cho:

- Settings nếu cần dữ liệu có cấu trúc.
- History.
- Các thông tin job đã hoàn thành nếu cần.

Các setting đơn giản có thể dùng JSON, nhưng phải giữ kiến trúc rõ ràng.

Không cần database server.

## 1.5 Logging

Phải có logging ứng dụng.

Có thể sử dụng thư viện logging phổ biến hoặc logging abstraction của .NET.

Log cần đủ để debug nhưng không ghi dữ liệu nhạy cảm không cần thiết.

---

# 2. QUY TẮC BẮT BUỘC CHO CODEX

Codex phải tuân thủ toàn bộ quy tắc dưới đây trong mọi bước.

## 2.1 Quy tắc điều phối

Khi người dùng nói:

- `tiếp`
- `bước tiếp theo`
- `prompt tiếp theo`
- `continue`
- hoặc câu tương đương

thì:

1. Đọc lại file master prompt này.
2. Tìm mục `PROJECT STATE`.
3. Xác định bước có trạng thái `[ ]` đầu tiên.
4. Kiểm tra các dependency của bước.
5. Thực hiện đúng bước đó.
6. Build/test.
7. Chỉ khi đạt acceptance criteria mới đổi `[ ]` thành `[x]`.
8. Cập nhật `LAST COMPLETED STEP`.
9. Cập nhật `NEXT STEP`.
10. Dừng lại và báo cáo ngắn gọn cho người dùng.

Mặc định mỗi lần `tiếp` chỉ thực hiện **một bước chính**.

Không tự chạy toàn bộ roadmap trong một lần.

## 2.2 Không nhảy bước

Không được:

- Bỏ qua bước chưa xong.
- Đánh dấu hoàn thành khi build đang lỗi.
- Đánh dấu hoàn thành khi chức năng chính chưa test.
- Làm bước 10 nếu bước 9 chưa đạt acceptance criteria, trừ khi người dùng yêu cầu rõ ràng.

## 2.3 Không phá code đang chạy

Trước khi sửa:

1. Đọc file liên quan.
2. Hiểu flow hiện tại.
3. Tận dụng code có sẵn.
4. Không rewrite cả module nếu chỉ cần sửa nhỏ.
5. Không đổi public contract vô cớ.
6. Không xóa feature đã chạy nếu chưa có lý do.

## 2.4 Token efficiency

Luôn ưu tiên tiết kiệm token.

Không:

- In lại toàn bộ file lớn nếu không cần.
- Giải thích dài dòng sau mỗi thay đổi.
- Scan toàn bộ repository ở mỗi bước.
- Đọc lại file không liên quan.
- Chạy command lặp lại vô ích.
- Tạo tài liệu dư thừa.

Nên:

- Dùng `git diff`.
- Dùng search theo symbol.
- Chỉ mở file liên quan tới bước hiện tại.
- Sau mỗi bước báo cáo ngắn:
  - Đã làm gì.
  - File chính đã sửa.
  - Build/test.
  - Bước kế tiếp.

## 2.5 Coding quality

Bắt buộc:

- Nullable reference types nếu project hỗ trợ.
- Async/await đúng cách.
- Không dùng `.Result` hoặc `.Wait()` trên UI thread.
- CancellationToken cho tác vụ dài.
- Dispose resource/process/stream đúng cách.
- Không swallow exception.
- Không `catch { }` rỗng.
- Không hard-code path người dùng.
- Không hard-code đường dẫn `C:\Users\...`.
- Không hard-code ffmpeg vào máy developer.
- Không block UI thread.
- Tách service/interface khi hợp lý.
- Tên class/method/property bằng tiếng Anh.
- Text UI có thể dùng tiếng Việt theo thiết kế sản phẩm.

## 2.6 Build gate

Sau MỌI bước có thay đổi code:

- Restore nếu cần.
- Build solution.
- Chạy test liên quan.
- Nếu có lỗi:
  - sửa lỗi trong scope bước hiện tại;
  - build lại;
  - chưa được đánh dấu hoàn thành cho tới khi pass.

Nếu lỗi thuộc môi trường bên ngoài và không thể sửa bằng code:

- ghi rõ nguyên nhân;
- đưa lệnh/ngắn gọn cách xử lý;
- không giả vờ step đã pass.

## 2.7 Git

Nếu repository đang dùng Git:

- Không force push.
- Không reset hard làm mất code người dùng.
- Không xóa thay đổi của người dùng.
- Có thể tạo commit sau mỗi milestone nếu người dùng yêu cầu.
- Nếu không được yêu cầu commit thì chỉ làm code và báo cáo diff.

## 2.8 Trạng thái kế hoạch

Chỉ được cập nhật trạng thái `[x]` sau khi acceptance criteria của step tương ứng đã đạt.

---

# 3. UI/UX MỤC TIÊU

Giao diện dựa trên app mẫu ban đầu nhưng nâng cấp theo hướng hiện đại hơn.

## 3.1 Main Window

Bố cục đề xuất:

### Header

- Tên: `VIDEO → MP3`
- Subtitle:
  `Chọn nhiều video hoặc dán nhiều link, mỗi đường dẫn nằm trên một dòng`

### Khu vực Input

Có:

- TextBox nhiều dòng cho:
  - URL.
  - đường dẫn file.
- Button:
  - `Chọn nhiều file...`
  - `Thêm link`
  - có thể dùng paste trực tiếp.
- Drag & Drop file vào cửa sổ.

Mỗi dòng input phải được parse thành một item.

### Output

- TextBox hiển thị thư mục output.
- Button `Chọn nơi lưu...`.

### Audio quality

ComboBox:

- Best/Recommended nếu phù hợp.
- 320 kbps.
- 256 kbps.
- 192 kbps.
- 128 kbps.

Mặc định:

**320 kbps**

### Job Queue

Thay vùng log lớn bằng bảng queue.

Các cột tối thiểu:

- Tên.
- Nguồn.
- Trạng thái.
- Tiến trình.
- Chất lượng.
- Output.
- Action.

Status có thể gồm:

- Waiting
- Analyzing
- Downloading
- Converting
- Completed
- Failed
- Canceled

### Footer action

Button:

- `CHUYỂN ĐỔI TẤT CẢ`
- `Hủy`
- `Xóa đã hoàn thành`
- `Mở thư mục`

Hiển thị:

- số job đã hoàn thành / tổng job.
- progress tổng.
- job đang xử lý.
- trạng thái tổng.

## 3.2 Context action trên job

Tối thiểu:

- Start/Retry.
- Cancel.
- Remove.
- Open output file.
- Open output folder.
- Copy source.
- View error.

## 3.3 Responsive trong phạm vi desktop

Main window:

- Có min width/min height hợp lý.
- Resize không làm vỡ layout.
- Queue chiếm phần không gian mở rộng.
- Button không bị đè.
- Text dài phải ellipsis hoặc tooltip.

---

# 4. DOMAIN MODEL

Model trung tâm:

`ConversionJob`

Các field gợi ý:

- `Id`
- `SourceType`
- `Source`
- `DisplayName`
- `InputFilePath`
- `SourceUrl`
- `OutputDirectory`
- `OutputFilePath`
- `RequestedBitrate`
- `Status`
- `Progress`
- `CurrentStage`
- `ErrorMessage`
- `CreatedAt`
- `StartedAt`
- `CompletedAt`
- `Duration`
- `ThumbnailUrl`
- `ThumbnailLocalPath`
- `Metadata`
- `RetryCount`

Không bắt buộc dùng đúng 100% field trên nếu thiết kế tốt hơn.

Enum:

`ConversionSourceType`

- LocalFile
- Url

Enum:

`ConversionJobStatus`

- Waiting
- Analyzing
- Downloading
- Converting
- Completed
- Failed
- Canceled

---

# 5. KIẾN TRÚC GỢI Ý

Solution có thể tổ chức như:

```text
VideoToMp3/
├─ src/
│  ├─ VideoToMp3.App/
│  ├─ VideoToMp3.Core/
│  └─ VideoToMp3.Infrastructure/
├─ tests/
│  └─ VideoToMp3.Tests/
├─ tools/
│  ├─ ffmpeg/
│  └─ yt-dlp/
└─ VideoToMp3.sln
```

Nếu app nhỏ và việc tách quá nhiều project tạo overhead thì có thể tinh giản, nhưng vẫn phải giữ separation:

- View.
- ViewModel.
- Model.
- Service.
- Infrastructure.

Các service dự kiến:

- `IInputParserService`
- `IMediaProbeService`
- `IFFmpegService`
- `IYtDlpService`
- `IConversionService`
- `IConversionQueueService`
- `ISettingsService`
- `IHistoryService`
- `IFileNameService`
- `INotificationService`

Có thể đổi tên nếu kiến trúc thực tế hợp lý hơn.

---

# 6. XỬ LÝ LOCAL FILE

Flow:

```text
Add local file
    ↓
Validate extension/file exists
    ↓
ffprobe
    ↓
Read media info
    ↓
Create job
    ↓
Queue
    ↓
FFmpeg
    ↓
MP3
    ↓
Completed
```

Các định dạng video phổ biến cần hướng tới:

- mp4
- mkv
- mov
- avi
- webm
- m4v
- mpg
- mpeg
- ts
- mts
- m2ts
- flv
- wmv

Không validate chỉ bằng extension.

Cần xử lý trường hợp:

- file không tồn tại.
- file hỏng.
- không có audio stream.
- output đã tồn tại.
- filename có Unicode.
- filename có khoảng trắng.
- filename rất dài.
- đường dẫn có ký tự đặc biệt.

---

# 7. XỬ LÝ URL

Flow:

```text
Paste URL
   ↓
Validate URL
   ↓
yt-dlp probe/info
   ↓
Create/update job metadata
   ↓
Download best suitable audio/media
   ↓
FFmpeg convert nếu cần
   ↓
MP3
```

Phải:

- truyền argument process an toàn.
- không ghép command shell kiểu dễ injection.
- đọc stdout/stderr bất đồng bộ.
- parse progress.
- hỗ trợ cancellation.
- kill process tree khi cancel nếu cần.
- không để process zombie.

URL lỗi phải trả về message dễ hiểu.

Ví dụ:

- URL không hợp lệ.
- nguồn không hỗ trợ.
- video unavailable.
- network error.
- yt-dlp lỗi.
- cần đăng nhập/cookie nhưng app chưa hỗ trợ.
- nội dung DRM/protected không xử lý.

---

# 8. FFMPEG

Không gọi FFmpeg qua một chuỗi command shell thiếu kiểm soát nếu API Process có thể truyền ArgumentList.

Nên dùng:

`ProcessStartInfo.ArgumentList`

nếu target framework hỗ trợ.

Yêu cầu:

- `UseShellExecute = false`
- Redirect stdout/stderr phù hợp.
- `CreateNoWindow = true`
- CancellationToken.
- Progress parser.
- Escape/path Unicode đúng.
- Không hiện cửa sổ console.

Output MP3 cần đảm bảo:

- format MP3.
- bitrate theo setting.
- audio hợp lệ.
- không giữ video stream.

Có thể dùng codec phổ biến:

`libmp3lame`

nếu FFmpeg build hỗ trợ.

---

# 9. YT-DLP

yt-dlp được xem là dependency ngoài.

App phải có abstraction để sau này dễ:

- update binary.
- đổi path.
- test fake implementation.

Không hard-code logic yt-dlp trực tiếp khắp ViewModel.

Tách `YtDlpService`.

Các nhiệm vụ:

- Get media info.
- Resolve title.
- Resolve duration.
- Resolve thumbnail.
- Download.
- Parse progress.
- Return result.

---

# 10. FILE NAMING

Cần có `FileNameService`.

Quy tắc:

- sanitize ký tự cấm trên Windows.
- giữ Unicode hợp lệ.
- trim dấu chấm/space cuối tên file.
- chống tên reserved:
  - CON
  - PRN
  - AUX
  - NUL
  - COM1...
  - LPT1...
- tránh path quá bất hợp lý.
- không overwrite file mặc định.

Nếu tồn tại:

```text
Song.mp3
Song (1).mp3
Song (2).mp3
```

Có thể thêm setting overwrite sau.

---

# 11. QUEUE ENGINE

Queue là phần quan trọng nhất.

Phải thiết kế độc lập với UI.

Chức năng:

- Add jobs.
- Start all.
- Cancel one.
- Cancel all.
- Retry one.
- Remove waiting/completed/failed job.
- Progress event.
- Status event.
- Aggregate progress.

Version đầu có thể xử lý tuần tự để ổn định.

Sau đó mới thêm parallel processing.

Không chạy quá nhiều FFmpeg/yt-dlp process cùng lúc.

Parallel mặc định sau này:

`2`

và cho Settings điều chỉnh trong giới hạn hợp lý.

---

# 12. SETTINGS

Settings tối thiểu:

- Output directory.
- Default bitrate.
- Theme.
- Max concurrent jobs.
- Auto open output folder after all completed.
- Notify when completed.
- Remember window size/position nếu hợp lý.
- Optional paths to ffmpeg/yt-dlp nếu dùng binary external.

Setting phải persist sau khi đóng app.

---

# 13. HISTORY

History lưu tối thiểu:

- source.
- title.
- output path.
- completed time.
- success/failure.
- error nếu có.
- requested bitrate.

History screen có:

- tìm kiếm.
- mở file.
- mở folder.
- xóa history.
- retry/re-add nếu source còn dùng được.

---

# 14. ERROR HANDLING

Không hiển thị raw stack trace cho người dùng cuối.

UI hiển thị message thân thiện.

Log giữ technical detail.

Ví dụ:

UI:

`Không thể chuyển đổi file vì video không có luồng âm thanh.`

Log:

- command.
- exit code.
- stderr summary.
- exception.

Tuy nhiên không log token/cookie/password nếu sau này app có auth.

---

# 15. TEST

Ít nhất phải có unit test cho các phần thuần logic:

- Input parser.
- File name sanitizer.
- Duplicate output name resolver.
- URL/local detection.
- Queue state transition nếu thiết kế cho phép.
- Progress parser.
- Settings serialization nếu cần.

Integration test FFmpeg/yt-dlp có thể tách riêng và skip khi binary không tồn tại.

---

# 16. RELEASE

Mục tiêu Windows x64.

Ưu tiên tạo release dễ chạy.

Có thể cân nhắc:

- self-contained.
- single-file nếu tương thích tốt.
- installer ở giai đoạn cuối.

Dependency FFmpeg/yt-dlp:

Phải chọn một chiến lược rõ ràng:

A. Bundle binary trong app distribution.

hoặc

B. Download dependency trong lần chạy đầu.

Mặc định cho bản đầu:

**Bundle cùng thư mục ứng dụng hoặc thư mục tools được app quản lý**, vì đơn giản và predictable.

Không commit binary cực lớn vào repository nếu policy repo không phù hợp. Có thể dùng script/download trong release pipeline ở giai đoạn sau.

---

# 17. PROJECT STATE

Codex phải cập nhật phần này sau mỗi bước.

`LAST COMPLETED STEP: 2`

`NEXT STEP: 3`

`CURRENT BLOCKER: None`

## Roadmap

- [x] STEP 01 - Khảo sát repository/môi trường và khởi tạo solution
- [x] STEP 02 - Tạo kiến trúc Core/App/Infrastructure/Test
- [ ] STEP 03 - Dựng Main Window UI cơ bản theo mockup
- [ ] STEP 04 - Tạo domain model ConversionJob và enum/state
- [ ] STEP 05 - Input parser cho nhiều file, nhiều URL và input trộn
- [ ] STEP 06 - File picker nhiều file + Drag & Drop
- [ ] STEP 07 - Settings + chọn output directory + bitrate
- [ ] STEP 08 - Dependency resolver cho FFmpeg/ffprobe/yt-dlp
- [ ] STEP 09 - ffprobe media analyzer cho local video
- [ ] STEP 10 - FFmpeg local video → MP3
- [ ] STEP 11 - Progress parser + progress UI cho local conversion
- [ ] STEP 12 - Queue engine tuần tự + Start All
- [ ] STEP 13 - Cancel job + Cancel All + process cleanup
- [ ] STEP 14 - Retry/Remove/Open file/Open folder
- [ ] STEP 15 - yt-dlp info/probe URL
- [ ] STEP 16 - Download URL + convert URL → MP3
- [ ] STEP 17 - URL download/conversion progress
- [ ] STEP 18 - Output naming, sanitize và duplicate resolver
- [ ] STEP 19 - Error handling + user-friendly errors + logging
- [ ] STEP 20 - Aggregate progress + trạng thái toàn queue
- [ ] STEP 21 - Settings persistence
- [ ] STEP 22 - History persistence + History UI
- [ ] STEP 23 - Metadata MP3 cơ bản
- [ ] STEP 24 - Thumbnail/cover art cho nguồn online
- [ ] STEP 25 - Playlist URL
- [ ] STEP 26 - Parallel queue có giới hạn
- [ ] STEP 27 - Notification + hoàn thiện UX
- [ ] STEP 28 - Theme sáng/tối + polish UI
- [ ] STEP 29 - Test suite + edge cases
- [ ] STEP 30 - Release build Windows x64
- [ ] STEP 31 - Installer/package
- [ ] STEP 32 - Final QA + cleanup + release checklist

---

# 18. CHI TIẾT TỪNG STEP

# STEP 01 - Khảo sát repository/môi trường và khởi tạo solution

## Mục tiêu

Xác định project hiện tại đã có gì và tạo nền móng nếu repository trống.

## Công việc

1. Kiểm tra:
   - file hiện có.
   - git status.
   - .NET SDK.
   - OS.
2. Không xóa code hiện có.
3. Nếu chưa có solution:
   - tạo `VideoToMp3.sln`.
4. Tạo `.gitignore` phù hợp nếu chưa có.
5. Xác định UI framework.
6. Mặc định dùng WPF.
7. Build project skeleton.

## Acceptance criteria

- Solution tồn tại.
- `dotnet build` pass.
- Không làm mất file của người dùng.
- PROJECT STATE được cập nhật.

---

# STEP 02 - Tạo kiến trúc Core/App/Infrastructure/Test

## Mục tiêu

Tách cấu trúc để code không biến thành một MainWindow khổng lồ.

## Công việc

Tạo project/layer cần thiết.

Thiết lập reference đúng chiều.

Ví dụ:

```text
App -> Core
App -> Infrastructure
Infrastructure -> Core
Tests -> Core
Tests -> Infrastructure khi cần
```

Không để Core phụ thuộc UI.

## Acceptance criteria

- Build pass.
- Không circular dependency.
- Có project test chạy được.

---

# STEP 03 - Dựng Main Window UI cơ bản theo mockup

## Mục tiêu

Tạo giao diện gần thiết kế mẫu.

## Thành phần

- Header.
- Input multiline.
- Choose files.
- Output folder.
- Bitrate ComboBox.
- Job grid/list.
- Convert all.
- Cancel.
- Open folder.
- Overall status.

Chưa cần chức năng conversion thật.

## Acceptance criteria

- App mở được.
- Resize ổn.
- Không crash.
- UI không nhồi logic xử lý media vào code-behind.

---

# STEP 04 - Domain model

Tạo:

- `ConversionJob`
- enums
- metadata model nếu cần.
- observable state phù hợp MVVM.

Không để domain phụ thuộc control WPF.

## Acceptance criteria

- Unit test/model compile.
- UI có thể bind danh sách job.

---

# STEP 05 - Input parser

Nhận text nhiều dòng.

Mỗi dòng có thể là:

- URL.
- local path.
- whitespace.

Yêu cầu:

- trim.
- bỏ dòng rỗng.
- chống duplicate trong một batch.
- phân loại URL/local.
- báo invalid input.

Không cần kiểm tra online network ở parser.

## Test

Có unit test cho:

- 1 URL.
- nhiều URL.
- local path.
- input trộn.
- duplicate.
- blank line.
- Unicode path.

---

# STEP 06 - Multi-file picker + Drag & Drop

Cho phép:

- chọn nhiều file.
- kéo nhiều file.
- append vào queue/input.
- không tạo duplicate vô lý.

Filter file picker theo nhóm video thông dụng, nhưng vẫn cho phép kiểm tra thực tế bằng ffprobe sau đó.

## Acceptance criteria

- Multi-select hoạt động.
- Drag/drop hoạt động.
- UI không freeze.

---

# STEP 07 - Output folder + bitrate Settings UI

Thực hiện:

- folder picker.
- default output.
- validate output path.
- bitrate 128/192/256/320.
- default 320.

Nếu output directory chưa tồn tại:

- cho phép tạo an toàn.

---

# STEP 08 - Dependency resolver

Tạo abstraction xác định đường dẫn:

- ffmpeg.
- ffprobe.
- yt-dlp.

Ưu tiên thư mục tools do app quản lý.

Khi binary thiếu:

- app báo rõ.
- không crash.

Có method kiểm tra version để diagnostics.

Không gọi network update ở step này.

---

# STEP 09 - ffprobe analyzer

Tạo service phân tích local media.

Lấy:

- duration.
- audio stream tồn tại hay không.
- container.
- optional title.

Không parse console bằng cách mong manh nếu ffprobe có JSON output.

Ưu tiên:

`ffprobe -print_format json ...`

Deserialize JSON.

## Acceptance criteria

- Video hợp lệ đọc được duration.
- File không audio được xác định.
- File lỗi trả result/error có cấu trúc.

---

# STEP 10 - Local → MP3

Tạo FFmpeg service.

Yêu cầu:

- async.
- CancellationToken.
- no console window.
- output path resolver.
- bitrate theo job.
- không overwrite mặc định.
- exit code được kiểm tra.
- stderr được capture.

## Acceptance criteria

Chuyển một local video mẫu thành MP3 thành công.

---

# STEP 11 - Local progress

FFmpeg cần xuất progress ở format parse được.

Ưu tiên cơ chế machine-readable nếu có thể.

Tính progress từ:

`processed time / duration`

Clamp 0..100.

UI update qua MVVM/thread-safe mechanism.

Không update UI hàng nghìn lần/giây.

Có throttle hợp lý.

---

# STEP 12 - Queue engine tuần tự

Tạo QueueService.

Version này:

- concurrency = 1.

Start All:

- waiting job chạy lần lượt.
- failed job không tự vô hạn retry.
- completed job bỏ qua.
- job mới thêm trong lúc queue chạy phải xử lý theo policy rõ ràng.

Queue logic không nằm trong View.

---

# STEP 13 - Cancellation

Hỗ trợ:

- cancel one.
- cancel all.

Nếu job đang có external process:

- gửi cancellation.
- terminate process.
- đảm bảo process tree được cleanup nếu cần.

Không để partial MP3 giả là completed.

Partial file:

- xóa hoặc đặt policy rõ ràng.

---

# STEP 14 - Job actions

Thêm:

- Retry.
- Remove.
- Open MP3.
- Open folder.
- Copy source.
- View error.

Action phải disabled đúng status.

---

# STEP 15 - yt-dlp probe URL

Tạo `YtDlpService`.

Probe URL lấy:

- title.
- duration.
- thumbnail.
- extractor/source nếu có.
- playlist flag nếu có.

Không download ở step này.

Nếu URL unsupported:

- status Failed với message rõ.

---

# STEP 16 - URL → MP3

Implement pipeline online:

1. Probe.
2. Download audio/media.
3. Convert to MP3 nếu cần.
4. Move/finalize output.
5. Cleanup temp.

Temp directory phải được quản lý.

Không dùng temp file name dễ collision.

Cancellation phải cleanup.

---

# STEP 17 - Online progress

Parse:

- downloading %.
- converting %.

Mapping progress tổng job có thể ví dụ:

- Analyze: 0-5%.
- Download: 5-70%.
- Convert: 70-99%.
- Completed: 100%.

Nếu nguồn không cần convert lại thì normalize progress hợp lý.

UI phải hiển thị stage:

- `Đang phân tích`
- `Đang tải`
- `Đang chuyển đổi`
- `Hoàn thành`

---

# STEP 18 - File naming

Implement service:

- sanitize.
- reserved names.
- duplicate numbering.
- Unicode.
- long filename.
- trailing period/space.

Unit test đầy đủ.

---

# STEP 19 - Errors + logging

Chuẩn hóa exception/result.

Tạo user-facing message.

Log technical detail.

Không popup spam khi 20 job lỗi.

Queue hiển thị từng lỗi.

Có thể có summary cuối batch.

---

# STEP 20 - Aggregate progress

Hiển thị:

- completed/total.
- failed.
- canceled.
- overall progress.
- current active job.
- queue status.

Overall progress không được nhảy vô lý khi job hoàn thành.

---

# STEP 21 - Persist Settings

Lưu:

- output path.
- bitrate.
- concurrency.
- theme.
- notification flags.

Settings corrupt:

- fallback default.
- không crash startup.

---

# STEP 22 - History

Tạo persistence.

History chỉ ghi job kết thúc.

UI có:

- danh sách.
- search.
- open file.
- open folder.
- clear.
- re-add/retry hợp lý.

Không để DB operation block UI.

---

# STEP 23 - Metadata MP3

Hỗ trợ metadata tối thiểu:

- Title.
- Artist nếu có.
- Album nếu có.
- Track nếu có.

Local file:

- giữ hoặc mapping metadata hợp lý nếu có.

Online:

- lấy metadata từ yt-dlp result khi có.

Không bịa metadata.

---

# STEP 24 - Thumbnail/Cover

Online video nếu có thumbnail:

- download thumbnail an toàn.
- nhúng cover vào MP3 bằng FFmpeg.
- cleanup temp image.

Nếu thumbnail lỗi:

- job vẫn có thể thành công mà không cover.
- log warning.

Có setting bật/tắt nếu phù hợp.

---

# STEP 25 - Playlist

Khi URL là playlist:

- probe.
- cho phép expand thành nhiều ConversionJob.

Không tạo vô hạn item.

UI báo số item.

Cho phép cancel.

Nếu playlist rất lớn:

- cần UX xác nhận hoặc giới hạn an toàn.

---

# STEP 26 - Parallel queue

Nâng queue từ 1 lên configurable concurrency.

Default:

`2`

Giới hạn hợp lý, ví dụ 1..4 hoặc theo thiết kế.

Không tạo một process FFmpeg cho tất cả CPU core vô kiểm soát.

Queue thread-safe.

Test race condition cơ bản.

---

# STEP 27 - Notification + UX

Thêm:

- Windows notification hoặc phương án phù hợp.
- completion summary.
- button state.
- keyboard usability.
- empty state.
- validation message.
- confirm khi cancel all nếu đang chạy.

Không notification spam từng job nếu batch quá lớn, trừ khi user chọn.

---

# STEP 28 - Theme + UI polish

Theme:

- Light.
- Dark.
- System nếu framework hỗ trợ hợp lý.

Polish:

- spacing.
- icon.
- progress bar.
- status chip.
- tooltip.
- row context menu.
- empty queue.
- error row.

Giữ tinh thần UI mẫu: đơn giản, sáng sủa, dễ hiểu.

---

# STEP 29 - Test suite + edge cases

Test các case:

1. 1 local MP4.
2. nhiều local file.
3. local không audio.
4. corrupt file.
5. Unicode filename.
6. filename dài.
7. URL hợp lệ.
8. URL lỗi.
9. network interruption.
10. cancel download.
11. cancel convert.
12. duplicate output.
13. app close khi job đang chạy.
14. output disk/path invalid.
15. thiếu ffmpeg.
16. thiếu yt-dlp.
17. settings corrupt.
18. playlist.
19. nhiều job parallel.

Sửa bug trong scope test.

---

# STEP 30 - Release Windows x64

Tạo publish profile/command.

Mục tiêu:

- Windows x64.
- Release.
- app chạy ngoài IDE.

Kiểm tra:

- dependency paths.
- tools.
- settings folder.
- logs.
- SQLite/history.

Không để app phụ thuộc path máy developer.

---

# STEP 31 - Installer/package

Tạo phương án phát hành.

Có thể:

- installer.
- portable zip.
- hoặc cả hai.

Phải có:

- version.
- app icon.
- install/uninstall sạch nếu installer.
- không cần admin nếu không thực sự cần.

---

# STEP 32 - Final QA

Cuối cùng:

1. Clean build.
2. Test Release.
3. Kiểm tra warnings quan trọng.
4. Kiểm tra dead code.
5. Kiểm tra TODO.
6. Kiểm tra log.
7. Kiểm tra app startup khi dependency lỗi.
8. Kiểm tra uninstall/portable.
9. Update PROJECT STATE.
10. Báo cáo release summary.

Chỉ khi pass mới đánh dấu project hoàn thành.

---

# 19. HÀNH VI KHI NGƯỜI DÙNG NÓI "TIẾP"

Ví dụ:

Người dùng:

```text
tiếp
```

Codex KHÔNG hỏi:

`Bạn muốn làm bước nào?`

Codex phải tự:

1. Đọc PROJECT STATE.
2. Thấy ví dụ:
   - STEP 01 `[x]`
   - STEP 02 `[x]`
   - STEP 03 `[ ]`
3. Thực hiện STEP 03.
4. Build/test.
5. Nếu pass:
   - đổi STEP 03 thành `[x]`.
   - `LAST COMPLETED STEP: 3`
   - `NEXT STEP: 4`
6. Trả lời ngắn:

```text
Đã hoàn thành STEP 03.

- Đã dựng Main Window.
- Đã thêm input/output/bitrate/job queue.
- Build: PASS.

Bước tiếp theo: STEP 04 - Domain model ConversionJob.
```

Sau đó DỪNG.

Không tự thực hiện STEP 04 nếu người dùng chưa nói tiếp.

---

# 20. HÀNH VI KHI STEP ĐANG LỖI

Nếu build/test fail:

Không được đánh dấu `[x]`.

Cập nhật:

`CURRENT BLOCKER`

Ví dụ:

```text
CURRENT BLOCKER: FFmpeg binary not found in tools/ffmpeg.
```

Nếu blocker có thể sửa bằng code trong scope:

- tự sửa.

Nếu cần người dùng cung cấp thứ gì đó:

- hỏi đúng một câu ngắn, chính xác.

Khi blocker được giải quyết và người dùng nói `tiếp`:

- tiếp tục STEP hiện tại.
- không nhảy sang step sau.

---

# 21. HÀNH VI KHI NGƯỜI DÙNG YÊU CẦU SỬA STEP CŨ

Nếu người dùng yêu cầu thay đổi chức năng đã hoàn thành:

1. Thực hiện yêu cầu đó trước.
2. Build/test.
3. Không tự đánh dấu step khác.
4. Sau khi hoàn thành, `NEXT STEP` vẫn là step chưa hoàn thành trước đó, trừ khi thay đổi làm roadmap cần điều chỉnh thực sự.

Không tạo roadmap mới.

---

# 22. QUY TẮC KHÔNG ĐƯỢC LÀM

Không:

- Dùng WebView để biến app thành website giả desktop nếu không cần.
- Dùng Electron.
- Thêm backend/server.
- Thêm login/account.
- Thêm cloud.
- Thêm analytics.
- Thêm quảng cáo.
- Thêm telemetry gửi ra ngoài.
- Thêm DRM bypass.
- Thêm cookie stealing.
- Thêm browser credential extraction.
- Thêm chức năng ngoài scope mà người dùng chưa yêu cầu.
- Rewrite framework giữa chừng.
- Đổi C# sang C++ giữa project nếu không có phê duyệt.
- Tạo một đống tài liệu không cần thiết.

---

# 23. APP CLOSE BEHAVIOR

Nếu người dùng đóng app khi đang xử lý:

Phải xử lý rõ:

- hỏi xác nhận.
- nếu xác nhận thoát:
  - cancel queue.
  - terminate process.
  - cleanup temp.
  - đóng app.

Không để ffmpeg/yt-dlp tiếp tục chạy ngầm.

---

# 24. SECURITY

Các process argument phải chống command injection.

Không:

```text
cmd.exe /c "yt-dlp " + userInput
```

nếu có cách an toàn hơn.

Ưu tiên trực tiếp executable + argument list.

Validate:

- URL scheme.
- output path.
- file path.

Không cho URL input biến thành arbitrary shell command.

Không trust title online để tạo filename trực tiếp mà không sanitize.

---

# 25. PERFORMANCE

Không load toàn bộ video vào RAM.

FFmpeg/yt-dlp xử lý stream/file trực tiếp.

Không đọc file video bằng `File.ReadAllBytes`.

Không giữ thumbnail khổng lồ trong memory lâu hơn cần thiết.

Virtualize job list nếu UI framework hỗ trợ và queue có thể lớn.

Throttle progress UI.

---

# 26. ACCESSIBILITY/USABILITY

- Tab order hợp lý.
- Button có text rõ.
- Status không chỉ dựa vào màu.
- Error có tooltip/detail.
- Progress có text %.
- Font dễ đọc.
- Hỗ trợ Windows scaling/DPI.

---

# 27. DEFAULT PRODUCT BEHAVIOR

Mặc định:

```text
Output:
%USERPROFILE%\Music\Video To MP3

Bitrate:
320 kbps

Concurrency:
1 ở giai đoạn đầu
2 sau STEP 26

Overwrite:
False

Embed thumbnail:
True sau khi STEP 24 hoàn thành

Notifications:
On sau STEP 27
```

Nếu folder default không dùng được, fallback an toàn sang Music hoặc Documents.

Không hard-code username.

---

# 28. ĐỊNH DẠNG BÁO CÁO SAU MỖI STEP

Codex chỉ cần trả lời ngắn theo format:

```text
Hoàn thành STEP XX - <Tên bước>

Đã làm:
- ...
- ...

File chính:
- ...
- ...

Kiểm tra:
- Build: PASS/FAIL
- Tests: PASS/FAIL

Bước tiếp theo:
STEP YY - <Tên bước>
```

Nếu có blocker:

```text
STEP XX chưa hoàn thành.

Blocker:
- ...

Đã thử:
- ...

Cần:
- ...
```

Không in lại toàn bộ master prompt.

---

# 29. START INSTRUCTION

Ngay khi Codex đọc file này lần đầu:

1. Đọc toàn bộ nội dung.
2. Kiểm tra PROJECT STATE.
3. Nếu STEP 01 chưa hoàn thành:
   - thực hiện STEP 01.
4. Không hỏi người dùng muốn bắt đầu từ đâu.
5. Không làm nhiều step trong một lượt.
6. Sau khi xong STEP 01, cập nhật file này.
7. Dừng và chờ người dùng nói `tiếp`.

---

# 30. FINAL DEFINITION OF DONE

Dự án chỉ được coi là hoàn thành khi:

- Local multi-file hoạt động.
- Multi-URL hoạt động.
- Mixed queue hoạt động.
- Drag/drop hoạt động.
- FFmpeg conversion ổn định.
- yt-dlp pipeline ổn định.
- Progress hoạt động.
- Cancel không để process ngầm.
- Retry hoạt động.
- Output naming an toàn.
- Settings persist.
- History hoạt động.
- Playlist hoạt động.
- Metadata/cover hoạt động.
- Parallel queue có giới hạn.
- UI được polish.
- Tests chính pass.
- Release x64 chạy ngoài IDE.
- Installer/package chạy được.
- Không phụ thuộc path máy developer.
- Không còn blocker nghiêm trọng.
- PROJECT STATE toàn bộ `[x]`.

---

# END OF MASTER PROMPT
