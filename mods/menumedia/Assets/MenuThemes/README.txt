MENU MEDIA - menu themes folder

Every background in this folder becomes ONE menu theme:

  beach.gif + beach.mp3   theme "beach": animated GIF with beach.mp3 as music
  night.png               theme "night": still image, normal game music
  beach_Icon.png          (optional) replaces the game's title logo in theme "beach"

- Backgrounds: .gif (animated), .png, .jpg
- Music (optional, same name as the background): .mp3 .ogg .wav .m4a .aac .flac
- Video (.mp4 etc.) is not opened: convert it first with convert-video.sh / convert-video.bat
  (writes name.gif + name.mp3).

After adding files: zip the mod again and import it in Bunny Loader.
manifest.json must be at the root of the ZIP.

Size and position: Settings > Mod Configuration > Menu Media.
Pick the theme in Settings > Menu Theme. The first time a new GIF is used it is
prepared in the background (the game may stutter a little); afterwards it is cached.

Optional config.json in this folder: language (auto/en/vi), fit (cover/contain/stretch),
padLeft/padRight/padTop/padBottom, areaScale, maxSide, maxFps (15), maxFrames, maxPixels,
preloadPerFrame, budgetMs. See the mod description.

------------------------------------------------------------------------------------

MENU MEDIA - thư mục theme menu (Tiếng Việt)

Mỗi nền trong thư mục này là MỘT theme menu:

  beach.gif + beach.mp3   theme "beach": GIF động, nhạc là beach.mp3
  night.png               theme "night": ảnh tĩnh, dùng nhạc mặc định của game
  beach_Icon.png          (tùy chọn) thay logo tiêu đề của game trong theme "beach"

- Nền: .gif (động), .png, .jpg
- Nhạc (tùy chọn, trùng tên với nền): .mp3 .ogg .wav .m4a .aac .flac
- Video (.mp4...) không được mở trực tiếp: hãy đổi trước bằng convert-video.sh / convert-video.bat
  (tạo name.gif + name.mp3).

Sau khi thêm file: nén lại mod thành ZIP rồi import vào Bunny Loader.
manifest.json phải nằm ở gốc ZIP.

Kích thước và vị trí: Cài đặt > Mod Configuration > Menu Media.
Chọn theme ở Cài đặt > Theme menu. Lần đầu dùng GIF mới, nó được chuẩn bị ở nền
(game có thể hơi giật); những lần sau đã có cache.
