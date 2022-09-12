create table if not exists com_settings(
    Id INTEGER PRIMARY KEY autoincrement,
    PortName VARCHAR(50),
    Connected INT DEFAULT 0,
    AddTimeStamp INT DEFAULT 0,
    AddNewLineWhenWrite INT DEFAULT 0,
    PortSetting VARCHAR(1000),
    WriteData VARCHAR(5000),
    CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')),
    UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')),
    unique(PortName)
);