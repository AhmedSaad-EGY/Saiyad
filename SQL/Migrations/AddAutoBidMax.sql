-- Migration: AddAutoBidMax
-- Adds MaxAutoBidAmount column to Bids table for auto-bid feature
-- Generated from EF Core migration: 20260515185733_AddAutoBidMax

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Bids]') 
    AND name = 'MaxAutoBidAmount'
)
BEGIN
    ALTER TABLE [dbo].[Bids] 
    ADD [MaxAutoBidAmount] decimal(18,2) NULL;
    
    PRINT 'SUCCESS: Added MaxAutoBidAmount column to Bids table.';
END
ELSE
BEGIN
    PRINT 'INFO: Column MaxAutoBidAmount already exists. Skipping.';
END
GO
