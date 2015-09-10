CREATE TABLE [dbo].[X5_wsModules] (
    [AutoCode]    INT            IDENTITY (1, 1) NOT NULL,
    [sModuleName] NVARCHAR (200) NOT NULL,
    [sLabelName]  NVARCHAR (200) NOT NULL,
    [sType]       NVARCHAR (50)  NULL,
    [sContent]    NVARCHAR (MAX) NULL,
    [dAddDate]    DATETIME       NULL,
    [bDel]        TINYINT        NULL
);

