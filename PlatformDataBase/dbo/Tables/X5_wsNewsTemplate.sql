CREATE TABLE [dbo].[X5_wsNewsTemplate] (
    [autocode]  INT            IDENTITY (1, 1) NOT NULL,
    [lSiteCode] INT            NOT NULL,
    [lType]     INT            NULL,
    [sName]     NVARCHAR (50)  NULL,
    [sContent]  NTEXT          NULL,
    [dTimeAdd]  DATETIME       NULL,
    [BDefault]  TINYINT        NULL,
    [sMemo]     NVARCHAR (100) NULL
);

