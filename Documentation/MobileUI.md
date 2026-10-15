# Giao diện điện thoại

`StudyRoomResponsiveUI` được tự gắn vào Canvas của màn hình bắt đầu và phòng khi chạy game hoặc đổi scene. Không cần chạy lại các builder hay sửa từng prefab.

- Canvas dùng chiều rộng tham chiếu 540 khi cầm dọc và chiều cao tham chiếu 540 khi cầm ngang. Các lớp giao diện được đặt trong `Screen.safeArea` để tránh tai thỏ và thanh điều hướng.
- Shop đổi cách xếp bộ lọc, tab và nút thao tác theo chiều màn hình. Danh sách sản phẩm, phòng và nhiệm vụ cuộn dọc.
- Cài đặt có vùng cuộn riêng để vẫn tới được nút lưu trên màn hình ngang thấp. Bảng cài đặt nằm trên HUD.
- Máy tính/nghe nhạc có bố cục dọc riêng, nút điều khiển lớn hơn và thanh tua/âm lượng có vùng chạm riêng với phần hiển thị.
- Hộp thoại đặt tên phòng dịch lên khi Unity cung cấp vùng bàn phím ảo.

## Kiểm tra

Kiểm tra trong Unity Play Mode với camera render vào các RenderTexture theo kích thước mục tiêu và vùng an toàn mô phỏng:

| Kích thước | Các bảng trong phòng |
| --- | --- |
| 320 × 568 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 360 × 640 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 390 × 844 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 412 × 915 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 430 × 932 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 540 × 720 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 640 × 360 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 768 × 1024 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 844 × 390 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 932 × 430 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |
| 1024 × 768 | Máy tính/nhạc, shop, nhiệm vụ, cài đặt |

44 kiểm tra giới hạn bảng trong phòng và 20 kiểm tra màn hình bắt đầu/chọn phòng/hộp thoại đều đạt. Kiểm tra thêm raycast UI đến nút phát, callback phát/tạm dừng và cuộn tới nút lưu cài đặt đều đạt. Ảnh kiểm tra được lưu tạm tại `Temp/PhoneUI/`.

Chưa kiểm tra trên thiết bị Android/iOS thật; bàn phím hệ thống và vùng an toàn thực tế cần kiểm tra thêm khi có bản build trên máy.
