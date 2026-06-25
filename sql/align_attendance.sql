BEGIN TRAN;
SET XACT_ABORT ON;

-- 1) Drop any foreign key that uses Attendance.Notes (if present)
DECLARE @fk sysname;
SELECT TOP (1) @fk = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.name = 'Attendance' AND c.name = 'Notes';

IF @fk IS NOT NULL
BEGIN
	PRINT 'Dropping FK: ' + @fk;
	EXEC('ALTER TABLE dbo.Attendance DROP CONSTRAINT [' + @fk + ']');
END
ELSE
	PRINT 'No FK found on Attendance.Notes';

-- 2) Ensure Notes column exists and is VARCHAR(500) NULL
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME = 'Notes')
BEGIN
	PRINT 'Altering Attendance.Notes to VARCHAR(500) NULL';
	ALTER TABLE dbo.Attendance ALTER COLUMN Notes VARCHAR(500) NULL;
END
ELSE
BEGIN
	PRINT 'Adding Attendance.Notes VARCHAR(500) NULL';
	ALTER TABLE dbo.Attendance ADD Notes VARCHAR(500) NULL;
END

-- 3) Ensure AttendanceDate is DATE NULL
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME = 'AttendanceDate')
BEGIN
	PRINT 'Altering Attendance.AttendanceDate to DATE NULL';
	ALTER TABLE dbo.Attendance ALTER COLUMN AttendanceDate DATE NULL;
END

-- 4) Ensure CheckInTime / CheckOutTime are TIME NULL
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME = 'CheckInTime')
BEGIN
	PRINT 'Altering Attendance.CheckInTime to TIME NULL';
	ALTER TABLE dbo.Attendance ALTER COLUMN CheckInTime TIME NULL;
END

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME = 'CheckOutTime')
BEGIN
	PRINT 'Altering Attendance.CheckOutTime to TIME NULL';
	ALTER TABLE dbo.Attendance ALTER COLUMN CheckOutTime TIME NULL;
END

-- 5) Ensure FK Attendance.EmployeeID -> Employees(EmployeeID) exists
IF NOT EXISTS (
	SELECT 1
	FROM sys.foreign_keys fk
	JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
	JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
	JOIN sys.tables t ON c.object_id = t.object_id
	WHERE t.name = 'Attendance' AND c.name = 'EmployeeID'
)
BEGIN
	PRINT 'Adding FK_Attendance_Employees';
	ALTER TABLE dbo.Attendance
	ADD CONSTRAINT FK_Attendance_Employees FOREIGN KEY (EmployeeID) REFERENCES dbo.Employees(EmployeeID);
END
ELSE
	PRINT 'FK from Attendance.EmployeeID already exists';

COMMIT;
PRINT 'Done.';
