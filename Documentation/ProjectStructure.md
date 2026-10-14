# Cấu trúc project StudyRoom

Project dùng Unity 6000.4.4f1 và Universal Render Pipeline (URP).

```text
Assets/
├── StudyRoom/                  Nội dung do project quản lý
│   ├── Scenes/                 ScreenStart.unity, SampleScene.unity
│   ├── Scripts/
│   │   ├── Runtime/
│   │   │   ├── Audio/          Âm thanh và âm lượng
│   │   │   ├── Camera/         Zoom khung cảnh
│   │   │   ├── Characters/     Điều khiển nhân vật
│   │   │   ├── Core/           Phiên làm việc, chuyển scene
│   │   │   ├── Data/           Kiểu dữ liệu catalog
│   │   │   ├── Environment/    Ngày giờ và ánh sáng
│   │   │   ├── Persistence/    Lưu tiến độ
│   │   │   ├── Rooms/          Nạp phòng
│   │   │   └── UI/             Menu và màn hình bắt đầu
│   │   └── Editor/Builders/    Công cụ tạo scene, UI và asset
│   ├── Prefabs/
│   │   ├── Characters/        Prefab nhân vật dùng trong game
│   │   └── Rooms/             Prefab phòng dùng trong game
│   ├── Data/Catalogs/         RoomCatalog.asset
│   ├── UI/
│   │   ├── Fonts/
│   │   ├── Sprites/
│   │   ├── Textures/Thumbnails/
│   │   └── RenderTextures/
│   └── Settings/
│       ├── Input/             Input Actions
│       └── Rendering/         URP, renderer, volume profiles
│           ├── Quality/       Cấu hình Low, Medium, High
│           └── Imported/      Cấu hình render đi kèm asset nhập
├── ThirdParty/                Asset nhập ngoài, giữ cấu trúc gói gốc
│   ├── Characters/Chibi_Girl/
│   ├── Mnostva_Art/
│   └── Documentation/
├── Plugins/                   Thư viện và plugin Unity
└── Templates/URP/             Readme và tài liệu template Unity

Packages/                      Manifest và khóa phiên bản package
ProjectSettings/               Thiết lập project của Unity
Documentation/                 Hướng dẫn project
Archives/UnityPackages/        Các gói cài đặt .unitypackage đã nhập
```

## Cách thêm nội dung mới

- Script chạy trong game nằm dưới `Scripts/Runtime`, theo chức năng.
- Script dùng `UnityEditor` nằm dưới `Scripts/Editor` để không đưa vào bản build.
- Scene và prefab của game nằm trong `StudyRoom/Scenes` và `StudyRoom/Prefabs`.
- Asset từ nhà cung cấp nằm dưới `ThirdParty`; prefab tùy chỉnh cho game nằm dưới `StudyRoom/Prefabs`.
- ScriptableObject chứa dữ liệu nằm trong `StudyRoom/Data`; cấu hình engine nằm trong `StudyRoom/Settings`.
- Các đường dẫn dùng bởi công cụ Editor được tập trung trong `StudyRoomEditorPaths.cs`. Khi đổi thư mục, cập nhật các hằng số tại đây.
- Di chuyển asset bằng cửa sổ Project hoặc `AssetDatabase.MoveAsset` để giữ GUID trong file `.meta`.
- Commit asset cùng file `.meta`, `Packages` và `ProjectSettings`.

`Library`, `Temp`, `Logs`, `UserSettings`, file `.csproj` và `.slnx` do Unity hoặc IDE tạo. Unity tiếp tục quản lý các thư mục này tại gốc project; `.gitignore` loại chúng khỏi quản lý phiên bản.

## Thay đổi từ cấu trúc cũ

- `Assets/MyGame` → `Assets/StudyRoom`.
- `MyGame/Scrip` → `StudyRoom/Scripts`, chia Runtime và Editor.
- `MyGame/asset` → `Assets/ThirdParty`.
- `Assets/Scenes` → `StudyRoom/Scenes`.
- `MyGame/StartScreen` → `StudyRoom/Prefabs`; catalog, thumbnail và render texture chuyển sang nhóm tương ứng.
- `Assets/Settings` và `Assets/URP Settings` → `StudyRoom/Settings/Rendering`.
- Các profile `Graphics_*` chuyển từ UI sang `Rendering/Quality`.
- Input Actions chuyển vào `StudyRoom/Settings/Input`.

Tên scene, tên lớp C# và GUID asset được giữ nguyên để tiếp tục dùng các liên kết hiện có.
