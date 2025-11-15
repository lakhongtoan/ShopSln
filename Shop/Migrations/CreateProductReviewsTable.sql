-- Script to create ProductReviews table
-- Run this script in your SQL Server database

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProductReviews]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ProductReviews] (
        [ReviewId] int IDENTITY(1,1) NOT NULL,
        [ProductId] bigint NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [UserName] nvarchar(100) NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([ReviewId])
    );

    CREATE INDEX [IX_ProductReviews_ProductId] ON [dbo].[ProductReviews] ([ProductId]);
    CREATE INDEX [IX_ProductReviews_UserId] ON [dbo].[ProductReviews] ([UserId]);

    ALTER TABLE [dbo].[ProductReviews] 
    ADD CONSTRAINT [FK_ProductReviews_Products_ProductId] 
    FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([ProductID]) ON DELETE CASCADE;
END
GO

