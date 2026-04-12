-- Tạo Database
CREATE DATABASE MusicASM_DB;
GO
USE MusicASM_DB;
GO

-- 1. Bảng Roles (Phân quyền)
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL -- Admin, User
);

-- 2. Bảng Users (Người dùng)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    AvatarUrl VARCHAR(255),
    IsPremium BIT DEFAULT 0, -- 0: Free, 1: Premium (Giống Spotify)
    CreatedAt DATETIME DEFAULT GETDATE(),
    RoleId INT NOT NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-- 3. Bảng Genres (Danh mục/Thể loại nhạc)
CREATE TABLE Genres (
    GenreId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ImageUrl VARCHAR(255)
);

-- 4. Bảng Artists (Nghệ sĩ)
CREATE TABLE Artists (
    ArtistId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Bio NVARCHAR(MAX),
    AvatarUrl VARCHAR(255)
);

-- 5. Bảng Albums (Album nhạc)
CREATE TABLE Albums (
    AlbumId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    ReleaseDate DATE,
    CoverImageUrl VARCHAR(255),
    ArtistId INT NOT NULL,
    FOREIGN KEY (ArtistId) REFERENCES Artists(ArtistId)
);

-- 6. Bảng Songs (Bài hát - Thực thể quan trọng nhất)
CREATE TABLE Songs (
    SongId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Duration INT NOT NULL, -- Thời lượng bài hát tính bằng giây
    FilePath VARCHAR(255) NOT NULL, -- Đường dẫn file nhạc (vd: /audio/song1.mp3)
    CoverImageUrl VARCHAR(255), -- Ảnh bìa bài hát
    ListenCount INT DEFAULT 0, -- Lượt nghe
    ReleaseDate DATE DEFAULT GETDATE(),
    GenreId INT NOT NULL,
    ArtistId INT NOT NULL,
    AlbumId INT NULL, -- Có thể NULL nếu bài hát phát hành dạng Single
    FOREIGN KEY (GenreId) REFERENCES Genres(GenreId),
    FOREIGN KEY (ArtistId) REFERENCES Artists(ArtistId),
    FOREIGN KEY (AlbumId) REFERENCES Albums(AlbumId)
);

-- 7. Bảng Playlists (Danh sách phát của người dùng)
CREATE TABLE Playlists (
    PlaylistId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    CoverImageUrl VARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UserId INT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- 8. Bảng PlaylistSongs (Bảng trung gian kết nối Playlist và Song)
CREATE TABLE PlaylistSongs (
    PlaylistId INT NOT NULL,
    SongId INT NOT NULL,
    AddedAt DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (PlaylistId, SongId),
    FOREIGN KEY (PlaylistId) REFERENCES Playlists(PlaylistId),
    FOREIGN KEY (SongId) REFERENCES Songs(SongId)
);
USE MusicASM_DB;
GO

-- 1. Thêm dữ liệu bảng Roles
INSERT INTO Roles (RoleName) VALUES 
('Admin'), 
('User');
GO

-- 2. Thêm dữ liệu bảng Users 
-- (Lưu ý: PasswordHash ở đây đang để text thuần '123456' cho dễ test, khi làm code C# bạn nhớ dùng thư viện BCrypt hoặc MD5 để mã hóa nhé)
INSERT INTO Users (Username, PasswordHash, FullName, Email, AvatarUrl, IsPremium, RoleId) VALUES 
('admin', '123456', N'Quản trị viên', 'admin@music-asm.com', '/images/avatars/admin.jpg', 1, 1),
('bao_dev', '123456', N'Bảo', 'bao@music.com', '/images/avatars/bao.jpg', 1, 2),
('duong_coder', '123456', N'Dương', 'duong@music.com', '/images/avatars/duong.jpg', 1, 2),
('lan_music', '123456', N'Lân', 'lan@music.com', '/images/avatars/lan.jpg', 0, 2),
('minh_pro', '123456', N'Minh', 'minh@music.com', '/images/avatars/minh.jpg', 0, 2),
('long_chill', '123456', N'Long', 'long@music.com', '/images/avatars/long.jpg', 1, 2);
GO

-- 3. Thêm dữ liệu bảng Genres
INSERT INTO Genres (Name, Description, ImageUrl) VALUES 
(N'Pop', N'Nhạc Pop thịnh hành', '/images/genres/pop.jpg'),
(N'Rap/Hip-Hop', N'Nhạc Rap sôi động', '/images/genres/rap.jpg'),
(N'Lofi Chill', N'Nhạc thư giãn, học tập', '/images/genres/lofi.jpg'),
(N'EDM', N'Nhạc điện tử', '/images/genres/edm.jpg');
GO

-- 4. Thêm dữ liệu bảng Artists
INSERT INTO Artists (Name, Bio, AvatarUrl) VALUES 
(N'Sơn Tùng M-TP', N'Nghệ sĩ nhạc Pop hàng đầu Việt Nam', '/images/artists/sontung.jpg'),
(N'HIEUTHUHAI', N'Rapper nổi bật với phong cách hiện đại', '/images/artists/hieuthuhai.jpg'),
(N'MCK', N'Rapper cá tính, âm nhạc đa màu sắc', '/images/artists/mck.jpg'),
(N'Đen Vâu', N'Rapper với những bản rap mộc mạc, ý nghĩa', '/images/artists/denvau.jpg');
GO

-- 5. Thêm dữ liệu bảng Albums
INSERT INTO Albums (Title, ReleaseDate, CoverImageUrl, ArtistId) VALUES 
(N'Chúng Ta', '2024-03-08', '/images/albums/chungta.jpg', 1),
(N'Ai cũng phải bắt đầu từ đâu đó', '2023-08-15', '/images/albums/aicungphai.jpg', 2),
(N'99%', '2023-03-02', '/images/albums/99percent.jpg', 3);
GO

-- 6. Thêm dữ liệu bảng Songs (Duration tính bằng giây)
INSERT INTO Songs (Title, Duration, FilePath, CoverImageUrl, ListenCount, GenreId, ArtistId, AlbumId) VALUES 
(N'Chúng Ta Của Tương Lai', 230, '/audio/chung-ta-cua-tuong-lai.mp3', '/images/songs/chungta.jpg', 1500000, 1, 1, 1),
(N'Ngủ Một Mình', 195, '/audio/ngu-mot-minh.mp3', '/images/songs/ngumotminh.jpg', 850000, 2, 2, 2),
(N'Chìm Sâu', 180, '/audio/chim-sau.mp3', '/images/songs/chimsau.jpg', 920000, 2, 3, 3),
(N'Trốn Tìm', 215, '/audio/tron-tim.mp3', '/images/songs/trontim.jpg', 1200000, 2, 4, NULL),
(N'Nơi Này Có Anh', 260, '/audio/noi-nay-co-anh.mp3', '/images/songs/noinaycoanh.jpg', 2500000, 1, 1, NULL);
GO

-- 7. Thêm dữ liệu bảng Playlists
INSERT INTO Playlists (Title, Description, CoverImageUrl, UserId) VALUES
(N'Nhạc Code Đêm Khuya', N'List nhạc chill để chạy deadline mượt mà', '/images/playlists/code-chill.jpg', 2),
(N'Top Hits V-Pop', N'Những bài hát thịnh hành nhất', '/images/playlists/vpop-hits.jpg', 3);
GO

-- 8. Thêm dữ liệu bảng PlaylistSongs (Thêm bài hát vào playlist)
-- Playlist 1 (Nhạc Code Đêm Khuya) thêm bài "Chìm Sâu" và "Trốn Tìm"
INSERT INTO PlaylistSongs (PlaylistId, SongId) VALUES 
(1, 3), 
(1, 4);

-- Playlist 2 (Top Hits V-Pop) thêm bài "Chúng Ta Của Tương Lai" và "Ngủ Một Mình"
INSERT INTO PlaylistSongs (PlaylistId, SongId) VALUES 
(2, 1), 
(2, 2);
GO



-- Bảng lưu bài hát yêu thích
CREATE TABLE FavoriteSongs (
    FavoriteId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    SongId INT NOT NULL,
    AddedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (SongId) REFERENCES Songs(SongId),
    CONSTRAINT UQ_User_Song UNIQUE (UserId, SongId)
);

-- Bảng lưu lịch sử nghe nhạc
CREATE TABLE ListeningHistory (
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    SongId INT NOT NULL,
    ListenedAt DATETIME DEFAULT GETDATE(),
    Duration INT, -- Số giây đã nghe
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (SongId) REFERENCES Songs(SongId)
);
select * from Songs
SELECT TOP 5 SongId, Title, FilePath, CoverImageUrl 
FROM Songs
UPDATE Songs
SET FilePath = '/music/default.mp3'
WHERE FilePath IS NULL
SELECT SongId, FilePath FROM Songs WHERE FilePath IS NULL
UPDATE Songs
SET ListenCount = 0
WHERE ListenCount IS NULL