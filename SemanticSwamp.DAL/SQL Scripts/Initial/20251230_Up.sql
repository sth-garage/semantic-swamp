USE SemanticSwamp
GO

/****** Object:  Table [dbo].[DocumentUploads]    Script Date: 12/18/2025 11:14:00 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DocumentUploads](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FileName] [varchar](500) NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[Base64Data] [varchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[HasBeenProcessed] [bit] NOT NULL,
 CONSTRAINT [PK_Uploads] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[DocumentUploads] ADD  CONSTRAINT [DF_Uploads_CreatedOn]  DEFAULT (getdate()) FOR [CreatedOn]
GO

ALTER TABLE [dbo].[DocumentUploads] ADD  CONSTRAINT [DF_DocumentUploads_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO

ALTER TABLE [dbo].[DocumentUploads] ADD  CONSTRAINT [DF_DocumentUploads_HasBeenProcessed]  DEFAULT ((0)) FOR [HasBeenProcessed]
GO


