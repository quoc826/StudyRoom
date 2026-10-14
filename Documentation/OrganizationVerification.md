# Kiểm tra sau khi sắp xếp thư mục

Ngày kiểm tra: 14/10/2026, trong Unity Editor.

- Unity biên dịch thành công.
- Giữ nguyên toàn bộ 2.838 GUID asset và thư mục có trong bản đối chiếu trước khi di chuyển.
- 3.018 dependency có GUID trước khi di chuyển tiếp tục được Unity tìm thấy.
- Hai scene của game và sáu prefab của game không có Missing Script.
- Catalog giữ đủ năm phòng, một nhân vật và các prefab/thumbnail tương ứng.
- Danh sách build trỏ tới `Assets/StudyRoom/Scenes/ScreenStart.unity` và `Assets/StudyRoom/Scenes/SampleScene.unity`, giữ tên scene và GUID cũ.
- 1.150 tham chiếu GUID trong asset của game được giải quyết thành công.
- Mã nguồn và cấu hình project đã được kiểm tra để loại đường dẫn thư mục cũ.

## Tham chiếu thiếu đã tồn tại trong cấu hình render

Các GUID dưới đây không nằm trong danh sách asset trước khi di chuyển và hiện cũng không tìm thấy trong AssetDatabase. Việc sắp xếp thư mục giữ nguyên các file cấu hình này; cần xử lý riêng nếu tiếp tục dùng các hiệu ứng render tương ứng.

| File | GUID không tìm thấy |
| --- | --- |
| `Assets/StudyRoom/Settings/Rendering/DefaultVolumeProfile.asset` | `60f3b30c03e6ba64d9a27dc9dba8f28d`, `0fd9ee276a1023e439cf7a9c393195fa`, `5a00a63fdd6bd2a45ab1f2d869305ffd`, `74955a4b0b4243bc87231e8b59ed9140` |
| `Assets/StudyRoom/Settings/Rendering/PC_Renderer.asset` | `e5c6678ed2aaa91408dd3df699057aae`, `03cfc4915c15d504a9ed85ecc404e607`, `53a11f4ebaebf4049b3638ef78dc9664`, `8f96cd657dc40064aa21efcc7e50a2e7`, `57d7c4c16e2765b47a4d2069b311bffe`, `24ec0e140fb444a44ab96ee80844e18e`, `b9a23f869c4fd45f19c5ada54dd82176` |
| `Assets/StudyRoom/Settings/Rendering/Imported/New Universal Render Pipeline Asset_Renderer.asset` | `60e1133243b97e347b653163a8c01b64` |

Kết quả trên xác nhận cấu trúc asset và khả năng biên dịch; không bao gồm tạo bản build hoặc chạy toàn bộ gameplay trong Play Mode.
