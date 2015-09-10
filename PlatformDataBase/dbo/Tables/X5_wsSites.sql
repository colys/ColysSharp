CREATE TABLE [dbo].[X5_wsSites] (
    [AutoCode]         INT            IDENTITY (1, 1) NOT NULL,
    [sName]            NVARCHAR (200) NOT NULL,
    [sDirectory]       NVARCHAR (200) NOT NULL,
    [lCompanyCode]     INT            NOT NULL,
    [lIndexTemplate]   INT            NULL,
    [lCommentTemplate] INT            NULL,
    [dAddDate]         DATETIME       NULL,
    [bDel]             TINYINT        NULL
);

