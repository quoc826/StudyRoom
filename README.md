# StudyRoom

Game phòng học và trang trí không gian cá nhân, xây dựng bằng Unity.

## Mở project

1. Cài Unity **6000.4.4f1** qua Unity Hub.
2. Clone repository và chọn **Add project from disk** trong Unity Hub.
3. Mở project, chờ Unity tải package và nhập asset.
4. Mở `Assets/StudyRoom/Scenes/ScreenStart.unity` rồi bấm Play.

Unity tự tạo lại `Library`, `Temp` và các file IDE; repository lưu `Assets` cùng `.meta`, `Packages` và `ProjectSettings`.

## Chức năng

- Chọn phòng và nhân vật, lưu các phòng đã tạo.
- Ngày giờ thực và ánh sáng thay đổi theo buổi sáng, chiều, đêm.
- Zoom bằng con lăn chuột hoặc chụm/tách hai ngón tay.
- Tự nhận 5 coin mỗi 15 phút trong game, kèm năm nhiệm vụ hằng ngày.
- Shop vật phẩm trang trí và danh sách Đồ của bạn.
- Đặt, xoay, thu vật phẩm về kho và lưu bố trí riêng của mỗi phòng.
- Âm thanh nút bấm và điều chỉnh âm lượng SFX/nhạc nền.

## Tài liệu

- [Cấu trúc project](Documentation/ProjectStructure.md)
- [Coin và nhiệm vụ](Documentation/CoinsAndDailyQuests.md)
- [Shop trang trí](Documentation/DecorationShop.md)
- [Kiểm tra cấu trúc và tham chiếu render có sẵn](Documentation/OrganizationVerification.md)

Asset nhập ngoài nằm trong `Assets/ThirdParty`; thư viện nằm trong `Assets/Plugins`. Các gói cài đặt `.unitypackage` được lưu cục bộ trong `Archives/UnityPackages`.
