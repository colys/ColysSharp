CREATE TABLE [dbo].[X5_wsNewCls] (
    [AutoCode]        INT            IDENTITY (1, 1) NOT NULL,
    [lSiteCode]       INT            NOT NULL,
    [sClsCode]        NVARCHAR (20)  NULL,
    [sClsFCode]       NVARCHAR (20)  NULL,
    [sClsName]        NVARCHAR (80)  NULL,
    [sClsMemo]        NVARCHAR (200) NULL,
    [lGrade]          INT            NULL,
    [bEnd]            TINYINT        NULL,
    [bDel]            TINYINT        NULL,
    [bViewEnd]        TINYINT        NULL,
    [lListTemplate]   INT            NULL,
    [lDetailTemplate] INT            NULL
);

