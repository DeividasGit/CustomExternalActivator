
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[MESSAGE__SEND]
(
    @messagetext nvarchar(max),
    @messagestatus tinyint = 0,
    @params nvarchar(max),
    @toolname nvarchar(100)
)
AS
SET NOCOUNT ON
SET TEXTSIZE 2147483647

IF @toolname is NULL
BEGIN
    EXEC E '@toolname is null', @@PROCID
    RETURN -1
END

DECLARE @messageid int = -1
DECLARE @starttime datetime2(0)

--Test server
IF @@SERVERNAME <> 'SQLSRV02'
BEGIN
    SET @params = ISNULL(@params, '') + ' -x'
END

INSERT INTO dbo.MESSAGE
(
    messagetext,
    messagestatus,
    params,
    toolname,
    sentdt
)
VALUES
(@messagetext, @messagestatus, @params, @toolname, GETUTCDATE())

SET @messageid = SCOPE_IDENTITY()
IF @messageid = -1
    RETURN -1

SET @starttime = GETDATE()

WHILE (
      (
          SELECT TOP (1)
              messagestatus
          FROM dbo.MESSAGE WITH (NOLOCK)
          WHERE ID = @messageid
      ) <> 1
      ) --response from external activator
      and (GETDATE() < DATEADD(SECOND, 45, @starttime)) --45 seconds timeout
BEGIN
    WAITFOR DELAY '00:00:05' --every 5 seconds
END

IF
(
    SELECT TOP (1)
        messagestatus
    FROM dbo.MESSAGE WITH (NOLOCK)
    WHERE ID = @messageid
) = 0
BEGIN
    UPDATE TOP (1)
        dbo.MESSAGE
    SET messagestatus = 2 --timeout
    FROM dbo.MESSAGE WITH (NOLOCK)
    WHERE ID = @messageid
END