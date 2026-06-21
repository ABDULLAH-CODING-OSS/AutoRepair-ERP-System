-- AddSalaryAdjustmentColumns.sql
-- Run this script against your application's database to add the missing columns
-- then restart the app. This is a one-time schema change to match the model.
-- Example: run in SSMS or sqlcmd using the same database used by ApplicationDbContext.

ALTER TABLE [dbo].[SalaryAdjustments]
ADD
	[AdjustmentStatus] VARCHAR(20) NULL,
	[IsActive] BIT NULL,
	[CreatedAt] DATETIME NULL;

-- Optional: if you prefer defaults for existing rows
-- UPDATE [dbo].[SalaryAdjustments] SET [IsActive] = 1 WHERE [IsActive] IS NULL;

-- Verify:
-- SELECT TOP 10 AdjustmentId, PayrollID, AdjustmentStatus, IsActive, CreatedAt FROM [dbo].[SalaryAdjustments];
