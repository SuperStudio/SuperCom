create table if not exists advanced_send(
    ProjectID INTEGER PRIMARY KEY autoincrement,
    ProjectName VARCHAR(200),
    Commands TEXT,
    CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')),
    UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime'))
);