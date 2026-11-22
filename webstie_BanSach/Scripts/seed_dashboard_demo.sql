/* Dashboard sample data - SQL Server
   Run in SSMS against your bookstore DB.
   1) USE [TenDatabase_Cua_Ban];
   2) Execute script.
   Idempotent-ish: inserts only when DEMO markers are missing.
*/

-- USE [TenDatabase_Cua_Ban];

SET NOCOUNT ON;
BEGIN TRAN;
SET XACT_ABORT ON;

DECLARE @now datetime2 = SYSUTCDATETIME();

-- Roles
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = N'ADMIN')
    INSERT INTO Roles(RoleName) VALUES (N'ADMIN');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = N'CUSTOMER')
    INSERT INTO Roles(RoleName) VALUES (N'CUSTOMER');

DECLARE @roleCustomer int = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = N'CUSTOMER');

-- Users (customers)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.customer1@bookstore.local')
    INSERT INTO Users(Email, Password, FullName, RoleId, IsActive, CreatedAtUtc)
    VALUES (N'demo.customer1@bookstore.local', N'demo', N'Nguyễn Văn A', @roleCustomer, 1, DATEADD(day,-40,@now));
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.customer2@bookstore.local')
    INSERT INTO Users(Email, Password, FullName, RoleId, IsActive, CreatedAtUtc)
    VALUES (N'demo.customer2@bookstore.local', N'demo', N'Trần Thị B', @roleCustomer, 1, DATEADD(day,-20,@now));
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.customer3@bookstore.local')
    INSERT INTO Users(Email, Password, FullName, RoleId, IsActive, CreatedAtUtc)
    VALUES (N'demo.customer3@bookstore.local', N'demo', N'Lê Văn C', @roleCustomer, 1, DATEADD(day,-10,@now));

DECLARE @u1 bigint = (SELECT TOP 1 UserId FROM Users WHERE Email = N'demo.customer1@bookstore.local');
DECLARE @u2 bigint = (SELECT TOP 1 UserId FROM Users WHERE Email = N'demo.customer2@bookstore.local');
DECLARE @u3 bigint = (SELECT TOP 1 UserId FROM Users WHERE Email = N'demo.customer3@bookstore.local');

-- Categories
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryCode = N'DEMO_KNS')
    INSERT INTO Categories(CategoryCode, Name, IsDeleted, UpdatedAtUtc, DeletedAtUtc)
    VALUES (N'DEMO_KNS', N'Kỹ năng sống', 0, @now, NULL);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryCode = N'DEMO_VH')
    INSERT INTO Categories(CategoryCode, Name, IsDeleted, UpdatedAtUtc, DeletedAtUtc)
    VALUES (N'DEMO_VH', N'Văn học', 0, @now, NULL);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryCode = N'DEMO_KT')
    INSERT INTO Categories(CategoryCode, Name, IsDeleted, UpdatedAtUtc, DeletedAtUtc)
    VALUES (N'DEMO_KT', N'Kinh tế', 0, @now, NULL);

DECLARE @cKns int = (SELECT TOP 1 CategoryId FROM Categories WHERE CategoryCode = N'DEMO_KNS');
DECLARE @cVh  int = (SELECT TOP 1 CategoryId FROM Categories WHERE CategoryCode = N'DEMO_VH');
DECLARE @cKt  int = (SELECT TOP 1 CategoryId FROM Categories WHERE CategoryCode = N'DEMO_KT');

-- Authors
IF NOT EXISTS (SELECT 1 FROM Authors WHERE AuthorName = N'Dale Carnegie' AND IsDeleted = 0)
    INSERT INTO Authors(AuthorName, IsDeleted, UpdatedAtUtc, DeletedAtUtc) VALUES (N'Dale Carnegie', 0, @now, NULL);
IF NOT EXISTS (SELECT 1 FROM Authors WHERE AuthorName = N'Paulo Coelho' AND IsDeleted = 0)
    INSERT INTO Authors(AuthorName, IsDeleted, UpdatedAtUtc, DeletedAtUtc) VALUES (N'Paulo Coelho', 0, @now, NULL);
IF NOT EXISTS (SELECT 1 FROM Authors WHERE AuthorName = N'Nguyễn Nhật Ánh' AND IsDeleted = 0)
    INSERT INTO Authors(AuthorName, IsDeleted, UpdatedAtUtc, DeletedAtUtc) VALUES (N'Nguyễn Nhật Ánh', 0, @now, NULL);
IF NOT EXISTS (SELECT 1 FROM Authors WHERE AuthorName = N'Robert Kiyosaki' AND IsDeleted = 0)
    INSERT INTO Authors(AuthorName, IsDeleted, UpdatedAtUtc, DeletedAtUtc) VALUES (N'Robert Kiyosaki', 0, @now, NULL);
IF NOT EXISTS (SELECT 1 FROM Authors WHERE AuthorName = N'James Clear' AND IsDeleted = 0)
    INSERT INTO Authors(AuthorName, IsDeleted, UpdatedAtUtc, DeletedAtUtc) VALUES (N'James Clear', 0, @now, NULL);

DECLARE @a1 int = (SELECT TOP 1 AuthorId FROM Authors WHERE AuthorName = N'Dale Carnegie' AND IsDeleted = 0);
DECLARE @a2 int = (SELECT TOP 1 AuthorId FROM Authors WHERE AuthorName = N'Paulo Coelho' AND IsDeleted = 0);
DECLARE @a3 int = (SELECT TOP 1 AuthorId FROM Authors WHERE AuthorName = N'Nguyễn Nhật Ánh' AND IsDeleted = 0);
DECLARE @a4 int = (SELECT TOP 1 AuthorId FROM Authors WHERE AuthorName = N'Robert Kiyosaki' AND IsDeleted = 0);
DECLARE @a5 int = (SELECT TOP 1 AuthorId FROM Authors WHERE AuthorName = N'James Clear' AND IsDeleted = 0);

-- Publishers
IF NOT EXISTS (SELECT 1 FROM Publishers WHERE PublisherName = N'NXB Trẻ' AND IsDeleted = 0)
    INSERT INTO Publishers(PublisherName, IsDeleted, UpdatedAtUtc, DeletedAtUtc) VALUES (N'NXB Trẻ', 0, @now, NULL);
IF NOT EXISTS (SELECT 1 FROM Publishers WHERE PublisherName = N'NXB Tổng Hợp' AND IsDeleted = 0)
    INSERT INTO Publishers(PublisherName, IsDeleted, UpdatedAtUtc, DeletedAtUtc) VALUES (N'NXB Tổng Hợp', 0, @now, NULL);

DECLARE @p1 int = (SELECT TOP 1 PublisherId FROM Publishers WHERE PublisherName = N'NXB Trẻ' AND IsDeleted = 0);
DECLARE @p2 int = (SELECT TOP 1 PublisherId FROM Publishers WHERE PublisherName = N'NXB Tổng Hợp' AND IsDeleted = 0);

-- Books (use ISBN13 starting 9990000000xxx as marker)
IF NOT EXISTS (SELECT 1 FROM Books WHERE ISBN13 = '9990000000001')
    INSERT INTO Books(ISBN13, Title, CategoryId, PublisherId, Price, Stock, CoverUrl, Description, Active, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc)
    VALUES ('9990000000001', N'Đắc nhân tâm', @cKns, @p2, 86000, 25, N'/Content/covers/00000000000001.jpg', N'Sách demo dashboard', 1, DATEADD(day,-60,@now), @now, 0, NULL);
IF NOT EXISTS (SELECT 1 FROM Books WHERE ISBN13 = '9990000000002')
    INSERT INTO Books(ISBN13, Title, CategoryId, PublisherId, Price, Stock, CoverUrl, Description, Active, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc)
    VALUES ('9990000000002', N'Nhà giả kim', @cVh, @p1, 79000, 2, NULL, N'Sách demo dashboard', 1, DATEADD(day,-55,@now), @now, 0, NULL);
IF NOT EXISTS (SELECT 1 FROM Books WHERE ISBN13 = '9990000000003')
    INSERT INTO Books(ISBN13, Title, CategoryId, PublisherId, Price, Stock, CoverUrl, Description, Active, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc)
    VALUES ('9990000000003', N'Cho tôi xin một vé đi tuổi thơ', @cVh, @p1, 80000, 8, NULL, N'Sách demo dashboard', 1, DATEADD(day,-50,@now), @now, 0, NULL);
IF NOT EXISTS (SELECT 1 FROM Books WHERE ISBN13 = '9990000000004')
    INSERT INTO Books(ISBN13, Title, CategoryId, PublisherId, Price, Stock, CoverUrl, Description, Active, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc)
    VALUES ('9990000000004', N'Dạy con làm giàu', @cKt, @p2, 110000, 1, NULL, N'Sách demo dashboard', 1, DATEADD(day,-45,@now), @now, 0, NULL);
IF NOT EXISTS (SELECT 1 FROM Books WHERE ISBN13 = '9990000000005')
    INSERT INTO Books(ISBN13, Title, CategoryId, PublisherId, Price, Stock, CoverUrl, Description, Active, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc)
    VALUES ('9990000000005', N'Atomic Habits', @cKns, @p2, 179000, 15, NULL, N'Sách demo dashboard', 1, DATEADD(day,-40,@now), @now, 0, NULL);

DECLARE @b1 bigint = (SELECT TOP 1 BookId FROM Books WHERE ISBN13='9990000000001');
DECLARE @b2 bigint = (SELECT TOP 1 BookId FROM Books WHERE ISBN13='9990000000002');
DECLARE @b3 bigint = (SELECT TOP 1 BookId FROM Books WHERE ISBN13='9990000000003');
DECLARE @b4 bigint = (SELECT TOP 1 BookId FROM Books WHERE ISBN13='9990000000004');
DECLARE @b5 bigint = (SELECT TOP 1 BookId FROM Books WHERE ISBN13='9990000000005');

-- BookAuthors
IF NOT EXISTS (SELECT 1 FROM BookAuthors WHERE BookId=@b1 AND AuthorId=@a1) INSERT INTO BookAuthors(BookId,AuthorId) VALUES(@b1,@a1);
IF NOT EXISTS (SELECT 1 FROM BookAuthors WHERE BookId=@b2 AND AuthorId=@a2) INSERT INTO BookAuthors(BookId,AuthorId) VALUES(@b2,@a2);
IF NOT EXISTS (SELECT 1 FROM BookAuthors WHERE BookId=@b3 AND AuthorId=@a3) INSERT INTO BookAuthors(BookId,AuthorId) VALUES(@b3,@a3);
IF NOT EXISTS (SELECT 1 FROM BookAuthors WHERE BookId=@b4 AND AuthorId=@a4) INSERT INTO BookAuthors(BookId,AuthorId) VALUES(@b4,@a4);
IF NOT EXISTS (SELECT 1 FROM BookAuthors WHERE BookId=@b5 AND AuthorId=@a5) INSERT INTO BookAuthors(BookId,AuthorId) VALUES(@b5,@a5);

-- Orders + OrderItems (marker OrderCode DEMO-DASH-xxx)
DECLARE @o1 bigint, @o2 bigint, @o3 bigint, @o4 bigint, @o5 bigint;

IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode='DEMO-DASH-001')
BEGIN
    INSERT INTO Orders(OrderCode,UserId,BuyerName,BuyerEmail,BuyerPhone,ShipAddress,StatusCode,PaymentStatusCode,PaymentMethodId,Subtotal,DiscountTotal,ShippingFee,CarrierName,TrackingNumber,ShipmentStatusCode,ShippedAtUtc,DeliveredAtUtc,CreatedAtUtc,CompletedAtUtc)
    VALUES('DEMO-DASH-001',@u1,N'Nguyễn Văn A',N'demo.customer1@bookstore.local',N'0900000001',N'HCM',N'COMPLETED',N'PAID',NULL,0,0,0,NULL,NULL,NULL,NULL,NULL,DATEADD(day,-1,@now),DATEADD(day,-1,@now));
    SET @o1 = SCOPE_IDENTITY();
    INSERT INTO OrderItems(OrderId,BookId,Quantity,UnitPrice) VALUES (@o1,@b1,3,86000),(@o1,@b5,1,179000);
END
ELSE SET @o1 = (SELECT TOP 1 OrderId FROM Orders WHERE OrderCode='DEMO-DASH-001');

IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode='DEMO-DASH-002')
BEGIN
    INSERT INTO Orders(OrderCode,UserId,BuyerName,BuyerEmail,BuyerPhone,ShipAddress,StatusCode,PaymentStatusCode,PaymentMethodId,Subtotal,DiscountTotal,ShippingFee,CarrierName,TrackingNumber,ShipmentStatusCode,ShippedAtUtc,DeliveredAtUtc,CreatedAtUtc,CompletedAtUtc)
    VALUES('DEMO-DASH-002',@u2,N'Trần Thị B',N'demo.customer2@bookstore.local',N'0900000002',N'HN',N'PENDING',N'UNPAID',NULL,0,0,0,NULL,NULL,NULL,NULL,NULL,DATEADD(day,-3,@now),NULL);
    SET @o2 = SCOPE_IDENTITY();
    INSERT INTO OrderItems(OrderId,BookId,Quantity,UnitPrice) VALUES (@o2,@b2,2,79000),(@o2,@b3,1,80000);
END
ELSE SET @o2 = (SELECT TOP 1 OrderId FROM Orders WHERE OrderCode='DEMO-DASH-002');

IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode='DEMO-DASH-003')
BEGIN
    INSERT INTO Orders(OrderCode,UserId,BuyerName,BuyerEmail,BuyerPhone,ShipAddress,StatusCode,PaymentStatusCode,PaymentMethodId,Subtotal,DiscountTotal,ShippingFee,CarrierName,TrackingNumber,ShipmentStatusCode,ShippedAtUtc,DeliveredAtUtc,CreatedAtUtc,CompletedAtUtc)
    VALUES('DEMO-DASH-003',@u3,N'Lê Văn C',N'demo.customer3@bookstore.local',N'0900000003',N'DN',N'COMPLETED',N'PAID',NULL,0,0,0,NULL,NULL,NULL,NULL,NULL,DATEADD(day,-7,@now),DATEADD(day,-6,@now));
    SET @o3 = SCOPE_IDENTITY();
    INSERT INTO OrderItems(OrderId,BookId,Quantity,UnitPrice) VALUES (@o3,@b1,2,86000),(@o3,@b4,1,110000);
END
ELSE SET @o3 = (SELECT TOP 1 OrderId FROM Orders WHERE OrderCode='DEMO-DASH-003');

IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode='DEMO-DASH-004')
BEGIN
    INSERT INTO Orders(OrderCode,UserId,BuyerName,BuyerEmail,BuyerPhone,ShipAddress,StatusCode,PaymentStatusCode,PaymentMethodId,Subtotal,DiscountTotal,ShippingFee,CarrierName,TrackingNumber,ShipmentStatusCode,ShippedAtUtc,DeliveredAtUtc,CreatedAtUtc,CompletedAtUtc)
    VALUES('DEMO-DASH-004',@u1,N'Nguyễn Văn A',N'demo.customer1@bookstore.local',N'0900000001',N'HCM',N'CANCELLED',N'UNPAID',NULL,0,0,0,NULL,NULL,NULL,NULL,NULL,DATEADD(day,-15,@now),NULL);
    SET @o4 = SCOPE_IDENTITY();
    INSERT INTO OrderItems(OrderId,BookId,Quantity,UnitPrice) VALUES (@o4,@b3,1,80000);
END
ELSE SET @o4 = (SELECT TOP 1 OrderId FROM Orders WHERE OrderCode='DEMO-DASH-004');

IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode='DEMO-DASH-005')
BEGIN
    INSERT INTO Orders(OrderCode,UserId,BuyerName,BuyerEmail,BuyerPhone,ShipAddress,StatusCode,PaymentStatusCode,PaymentMethodId,Subtotal,DiscountTotal,ShippingFee,CarrierName,TrackingNumber,ShipmentStatusCode,ShippedAtUtc,DeliveredAtUtc,CreatedAtUtc,CompletedAtUtc)
    VALUES('DEMO-DASH-005',@u2,N'Trần Thị B',N'demo.customer2@bookstore.local',N'0900000002',N'HN',N'COMPLETED',N'PAID',NULL,0,0,0,NULL,NULL,NULL,NULL,NULL,DATEADD(day,-25,@now),DATEADD(day,-24,@now));
    SET @o5 = SCOPE_IDENTITY();
    INSERT INTO OrderItems(OrderId,BookId,Quantity,UnitPrice) VALUES (@o5,@b5,2,179000),(@o5,@b2,1,79000);
END
ELSE SET @o5 = (SELECT TOP 1 OrderId FROM Orders WHERE OrderCode='DEMO-DASH-005');

-- Recompute Subtotal/Discount/Shipping for demo orders (GrandTotal is computed by DB)
;WITH t AS (
    SELECT o.OrderId,
           SUM(oi.Quantity * oi.UnitPrice) AS Subtotal
    FROM Orders o
    JOIN OrderItems oi ON oi.OrderId = o.OrderId
    WHERE o.OrderCode LIKE 'DEMO-DASH-%'
    GROUP BY o.OrderId
)
UPDATE o
SET o.Subtotal = t.Subtotal,
    o.DiscountTotal = 0,
    o.ShippingFee = 0
FROM Orders o
JOIN t ON t.OrderId = o.OrderId;

COMMIT;

SELECT 'Dashboard demo data inserted/updated.' AS Result;
