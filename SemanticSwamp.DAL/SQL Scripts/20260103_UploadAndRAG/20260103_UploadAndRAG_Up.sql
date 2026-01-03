USE SemanticSwamp
GO

/****** Object:  Table [dbo].[Terms]    Script Date: 1/3/2026 3:06:34 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Terms](
	[Id] [int] NOT NULL,
	[Name] [varchar](2000) NOT NULL,
 CONSTRAINT [PK_Terms] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO




/****** Object:  Table [dbo].[Categories]    Script Date: 1/3/2026 3:06:17 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Categories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](2000) NOT NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


/****** Object:  Table [dbo].[Collections]    Script Date: 1/3/2026 3:05:46 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Collections](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](2000) NOT NULL,
 CONSTRAINT [PK_Collections] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
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


USE [SemanticSwamp]
GO

/****** Object:  Table [dbo].[DocumentUploadTerms]    Script Date: 1/3/2026 3:05:06 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DocumentUploadTerms](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TermId] [int] NOT NULL,
	[DocumentUploadId] [int] NOT NULL,
 CONSTRAINT [PK_DocumentUploadTerms] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DocumentUploadTerms]  WITH CHECK ADD  CONSTRAINT [FK_DocumentUploadTerms_Terms] FOREIGN KEY([TermId])
REFERENCES [dbo].[Terms] ([Id])
GO

ALTER TABLE [dbo].[DocumentUploadTerms] CHECK CONSTRAINT [FK_DocumentUploadTerms_Terms]
GO





