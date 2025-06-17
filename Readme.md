##### 搁置大文件提交github

```
# 本次临时搁置
git reset HEAD **/XmindToExcelConverter.exe
git add .
git commit -m "更新"
git push
# 永久忽略
touch .gitignore
XmindToExcelConverter.exe
git rm --cached **/XmindToExcelConverter.exe
git add .gitignore
git commit -m "忽略所有 exe 文件
git push
```