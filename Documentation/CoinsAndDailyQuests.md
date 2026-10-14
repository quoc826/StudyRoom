# Coin và nhiệm vụ hằng ngày

## Kiếm coin

- Mỗi 15 phút game đang chạy: tự cộng 5 coin vào ví.
- Thanh coin/nhiệm vụ ở góc trên bên trái hiển thị ví và thời gian còn lại đến lần cộng tiếp theo. Trong phòng, thanh ngày giờ nằm ở góc dưới bên trái.
- Khi được cộng coin, hiện thông báo `+5 coin đã vào ví!`.
- Coin dùng chung với hệ thống mở khóa phòng và nhân vật.
- Thời gian chưa đủ 15 phút được lưu, tiếp tục khi mở lại hoặc chuyển scene.
- Game desktop tiếp tục tính khi chạy nền để người chơi có thể học ở ứng dụng khác. Không cộng thời gian khi đóng game hoặc ứng dụng bị tạm ngưng; khoảng gián đoạn dài giữa các frame bị giới hạn để không tính thời gian máy ngủ.

## Năm nhiệm vụ mỗi ngày

| Nhiệm vụ | Điều kiện | Thưởng |
| --- | --- | --- |
| Điểm danh hôm nay | Mở game trong ngày | 5 coin |
| Học bài trong 30 phút | Tích lũy 30 phút bật chế độ học trong phòng | 10 coin |
| Ở trong game 60 phút | Tích lũy 60 phút game chạy | 15 coin |
| Học tổng cộng 60 phút | Tích lũy 60 phút bật chế độ học trong phòng | 20 coin |
| Thử thách trong game 2 tiếng | Tích lũy 120 phút game chạy | 30 coin |

Mở **Nhiệm vụ** trên thanh coin để xem tiến độ. Trong phòng, bấm **Bắt đầu học** để bật tính thời gian học; bấm **Tạm dừng học** khi nghỉ. Quay về màn hình bắt đầu sẽ dừng chế độ học. Thời gian học có thể tích lũy qua nhiều lần bắt đầu/tạm dừng trong ngày; game không kiểm chứng hoạt động học ngoài ứng dụng.

Khi đủ điều kiện, bấm **Nhận +…** tại nhiệm vụ tương ứng. Mỗi nhiệm vụ nhận một lần trong ngày. Thưởng nhiệm vụ cộng thêm vào thưởng mỗi 15 phút. Ví dụ, ở trong game đủ hai giờ và học đủ một giờ trong ngày sẽ kiếm được 40 coin tự động và có thể nhận 80 coin từ năm nhiệm vụ.

## Lưu tiến độ và đổi ngày

- Dùng ngày trên thiết bị, làm mới nhiệm vụ lúc 00:00 hoặc lần mở game tiếp theo.
- Đổi ngày giữ nguyên coin và tiến độ chu kỳ 15 phút; làm mới tiến độ học/online và trạng thái nhận thưởng của năm nhiệm vụ.
- Nhiệm vụ chưa nhận thưởng của ngày trước không chuyển sang ngày mới.
- Số coin, trạng thái nhận thưởng và thời gian được lưu chung trong `StudyRoom.Progress.v1`; dữ liệu save phiên bản 1 được nâng cấp mà vẫn giữ coin, phòng và các mục đã mở khóa.
- Tự lưu thời gian mỗi 30 giây, khi cộng/nhận coin, chuyển scene, tạm ngưng hoặc thoát game. Nếu ứng dụng bị đóng cưỡng bức, có thể mất tối đa khoảng 30 giây chưa lưu.

## Mã nguồn và kiểm tra

- `Scripts/Runtime/Data/StudyRoomDailyQuests.cs`: năm nhiệm vụ và mức thưởng.
- `Scripts/Runtime/Persistence/StudyRoomProgress.cs`: dữ liệu, chu kỳ thưởng và nhận thưởng một lần.
- `Scripts/Runtime/Core/StudyRoomEconomy.cs`: ví và bộ đếm dùng chung qua các scene.
- `Scripts/Runtime/UI/StudyRoomRewardsUI.cs`: thanh coin, bảng nhiệm vụ và chế độ học.

Đã kiểm tra 47 điều kiện logic bằng save thử riêng, gồm mốc thời gian, lưu/khôi phục, đổi ngày, nhận thưởng lặp, nâng cấp save và mua phòng. Đã kiểm tra tích hợp nút nhiệm vụ, bắt đầu/tạm dừng học, nhận thưởng, cập nhật ví, thông báo +5 coin và loại thời gian tạm ngưng/máy ngủ. Giao diện được kiểm tra qua ảnh render Unity. HUD và bảng nhiệm vụ chỉ xuất hiện trong scene phòng; quay về Home sẽ ẩn chúng, còn ví và tiến độ vẫn được giữ.
