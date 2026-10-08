:on Error exit

-- TODO: run on a disposable DB
USE Warehouse;
GO

SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @TestsRun INT = 0;
DECLARE @TestsPassed INT = 0;

BEGIN TRANSACTION;

BEGIN TRY
:r .\tests\integrity\000_setup_fixtures.sql
:r .\tests\integrity\001_foreign_key_tests.sql
:r .\tests\integrity\002_unique_constraint_tests.sql
:r .\tests\integrity\003_check_constraint_tests.sql
:r .\tests\integrity\004_rowversion_tests.sql

	ROLLBACK TRANSACTION;

	PRINT CONCAT(
		'Integrity tests passed: ',
		@TestsPassed,
		'/',
		@TestsRun
	);
END TRY
BEGIN CATCH
	IF XACT_STATE() <> 0
		ROLLBACK TRANSACTION;

	THROW;
END CATCH;
GO