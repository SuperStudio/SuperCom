# 语法高亮

参考：http://avalonedit.net/documentation/html/4d4ceb51-154d-43f0-b876-ad9640c5d2d8.htm

示例

```xml
<!-- 时间戳 -->
<!-- [2022-09-29 23:28:19.967] -->
<RuleSet >
    <Rule foreground="#858585">\[\d\d\d\d-\d\d-\d\d\ \d\d:\d\d:\d\d\.\d\d\d\]</Rule>
    <Span foreground="#FF4040" multiline="true">
        <Begin>(\[E\])|E:</Begin>
        <End>^(?=\[\d\d\d\d-\d\d-\d\d\ \d\d:\d\d:\d\d\.\d\d\d\])</End>
    </Span>
    <Span foreground="#FFEC8B" multiline="true">
        <Begin>\[W\]|W:</Begin>
        <End>^(?=\[\d\d\d\d-\d\d-\d\d\ \d\d:\d\d:\d\d\.\d\d\d\])</End>
    </Span>
    <Span foreground="#BFEFFF" multiline="true">
        <Begin>\[D\]|D:</Begin>
        <End>^(?=\[\d\d\d\d-\d\d-\d\d\ \d\d:\d\d:\d\d\.\d\d\d\])</End>
    </Span>

    <Span foreground="#32CD32" begin="\{" end="\}"/>

    <Keywords foreground="#63B8FF" fontWeight="bold">
        <Word>SEND >>>>>>>>>></Word>
    </Keywords>
</RuleSet>
```

示例

```xml
<RuleSet >
    <Span foreground="Green" begin="\{" end="\}"/>
</RuleSet>

<Color name="Bool" foreground="Blue" exampleText="true | false" />
<Color name="Number" foreground="Red" exampleText="3.14" />
<Color name="String" foreground="Green" exampleText="" />
<Color name="Null" foreground="Olive" exampleText="" />
<Color name="FieldName" foreground="DarkMagenta" />

<RuleSet name="String">
    <Span begin="\\" end="."/>
</RuleSet>

<RuleSet name="Object">
    <Span color="FieldName" ruleSet="String">
        <Begin>"</Begin>
        <End>"</End>
    </Span>
    <Span color="FieldName" ruleSet="String">
        <Begin>'</Begin>
        <End>'</End>
    </Span>
</RuleSet>

<RuleSet name="Expression">
    <Keywords color="Bool" >
        <Word>true</Word>
        <Word>false</Word>
    </Keywords>
    <Keywords color="Null" >
        <Word>null</Word>
    </Keywords>
    <Span color="String" ruleSet="String">
        <Begin>"</Begin>
        <End>"</End>
    </Span>
    <Span color="String" ruleSet="String">
        <Begin>'</Begin>
        <End>'</End>
    </Span>
</RuleSet>

<RuleSet>
    <Import ruleSet="Expression"/>
</RuleSet>
```

