-- ======================================================================
-- Fix_BadImageUrls.sql
-- Description: Strips the stale absolute Vercel domain prefix from all
--              image URL columns. The FileStorage service correctly stores
--              relative paths (/api/images/...), but the DB was populated
--              with full absolute URLs (https://saiyad-eg.vercel.app/...)
--              during a previous deployment cycle.
--
-- Executes within a transaction with explicit ROLLBACK on error.
-- Does NOT touch any business logic, financial records, or auth data.
-- ======================================================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. ProductImages.ImageUrl — primary product photos
    UPDATE [dbo].[ProductImages]
    SET [ImageUrl] = REPLACE([ImageUrl], N'https://saiyad-eg.vercel.app', N'')
    WHERE [ImageUrl] LIKE N'https://saiyad-eg.vercel.app%';

    -- 2. AuctionRequests.ProductImageUrl — auction request images (nullable)
    UPDATE [dbo].[AuctionRequests]
    SET [ProductImageUrl] = REPLACE([ProductImageUrl], N'https://saiyad-eg.vercel.app', N'')
    WHERE [ProductImageUrl] LIKE N'https://saiyad-eg.vercel.app%';

    -- 3. Users.ProfileImage — user avatar/profile pictures (nullable)
    UPDATE [dbo].[Users]
    SET [ProfileImage] = REPLACE([ProfileImage], N'https://saiyad-eg.vercel.app', N'')
    WHERE [ProfileImage] LIKE N'https://saiyad-eg.vercel.app%';

    COMMIT TRANSACTION;
    PRINT 'OK: All bad image URLs sanitized successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    RAISERROR(N'FATAL: Image URL sanitization failed — %s', @ErrorSeverity, 1, @ErrorMessage);
END CATCH;
GO
