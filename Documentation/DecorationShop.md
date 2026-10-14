# Shop trang trí phòng

Nút **Shop** nằm dưới Menu ở góc trên bên phải của phòng.

Nút **Đồ của bạn** nằm bên dưới Shop, mở thẳng danh sách vật phẩm đã mua. Danh sách hiển thị số lượng trong kho và số lượng đã đặt trong các phòng; chọn **Đặt vào phòng** để dùng một bản đang có trong kho. Vật phẩm chưa mua không xuất hiện ở đây. Có thể chuyển giữa các tab Cửa hàng, Đồ của bạn và Đồ trong phòng.

## Mua và đặt đồ

1. Mở Shop, cuộn hoặc vuốt danh sách để xem vật phẩm.
2. Bấm **Mua · [giá]**. Coin được trừ và một bản vật phẩm được thêm vào kho. Có thể mua nhiều bản của cùng một món.
3. Bấm **Đặt vào phòng**. Shop đóng và vật phẩm xem trước xuất hiện.
4. Trên máy tính, di chuyển chuột đến vị trí trên sàn. Trên màn hình cảm ứng, chạm/kéo một ngón tay để chọn vị trí.
5. Bấm **Xoay 90°** nếu cần, sau đó **Đặt** để xác nhận. Bấm **Hủy** hoặc Escape để hủy; vật phẩm vẫn ở trong kho.

Màu xanh báo vị trí hợp lệ, màu đỏ báo vị trí không hợp lệ. Vật phẩm phải nằm trọn trên sàn; đồ có chiều cao không được chồng lên nội thất khác hoặc nhân vật. Thảm mỏng có thể nằm bên dưới nội thất.

Mở tab **Đồ trong phòng** và chọn **Thu về kho** để lấy lại vật phẩm đã đặt từ shop. Sau đó có thể đặt lại ở vị trí khác. Thu về kho trả lại vật phẩm, không hoàn coin.

## Danh mục ban đầu

| Vật phẩm | Coin |
| --- | ---: |
| Cây xanh nhỏ | 10 |
| Đèn đứng ấm áp | 20 |
| Ghế đôn êm | 15 |
| Bình hoa xinh | 10 |
| Bàn ngôi sao | 25 |
| Gấu bông | 5 |
| Tủ sách | 35 |
| Thảm mềm | 20 |

## Dữ liệu và công cụ

- `Assets/StudyRoom/Data/Catalogs/DecorationShopCatalog.asset`: vật phẩm, giá, prefab và ảnh.
- `Assets/StudyRoom/UI/Textures/Shop`: ảnh xem trước được tạo từ prefab.
- `StudyRoomProgress`: kho chung và bố trí riêng của mỗi phòng, lưu cùng ví coin trong save hiện có.
- `StudyRoomDecorations`: kiểm tra vị trí, xem trước, xoay, đặt, thu về và nạp lại bố trí.
- `StudyRoomShopUI`: nút Shop và giao diện cửa hàng/kho/đồ trong phòng.
- Menu Editor **StudyRoom → Create Decoration Shop** tạo catalog ban đầu nếu chưa có và tạo giao diện trong scene phòng.

Save phiên bản 1/2 được nâng cấp giữ nguyên coin, phòng, đồ đã mở khóa và nhiệm vụ. Mua/đặt/thu về được lưu ngay. Không bán nội thất đã có sẵn trong prefab phòng.

Đã kiểm tra 39 điều kiện về mua, thiếu coin, kho, đặt ngoài sàn, vị trí hợp lệ, xoay, hủy, nạp lại bố trí, thu về kho và save cũ bằng dữ liệu thử riêng; save người chơi không bị thay đổi. Giao diện được kiểm tra qua ảnh render Unity.
