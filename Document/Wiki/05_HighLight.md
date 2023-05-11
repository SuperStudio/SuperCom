# 语法高亮

可自定义语法高亮

打开一个串口后，点击语法高亮编辑按钮，或者工具-设置-语法高亮，进行编辑与新增

> SuperCom 默认集成语法高亮规则 ComLog

<img src="https://s1.ax1x.com/2023/05/10/p9rm3IU.png" alt="image-20230510231234882" style="zoom:80%;" />

# 新增规则

按照如下新增规则

<img src="https://s1.ax1x.com/2023/05/10/p9rmGiF.png" alt="image-20230510231552820" style="zoom:80%;" />

---

**常见规则高亮形式**

时间戳

```
\[\d\d\d\d-\d\d-\d\d\ \d\d:\d\d:\d\d\.\d\d\d\]
```

JSON
```                             
\{.+\}
```


匹配形如 E: xxxx 或 [E]:xxxx 的日志 

```
(\bE: .+)|(\[E\]:.+)
```


匹配纯数字

```
\b0[xX][0-9a-fA-F]+|(\b\d+(\.[0-9]+)?|\.[0-9]+)([eE][+-]?[0-9]+)?
```

效果如下

<img src="https://s1.ax1x.com/2023/05/10/p9rmYRJ.png" alt="image-20230510232228180" style="zoom:80%;" />







