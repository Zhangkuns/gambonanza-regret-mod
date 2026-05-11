Gambonanza Save Manager
=======================

功能：
- 选择/识别 Gambonanza 的 save.json
- 开启自动备份：检测 save.json 变化后自动复制备份
- 手动备份
- 备份列表：显示状态、波次、金币、白棋/黑棋数量
- 右侧 5x8 棋盘 UI：显示每个备份或当前存档的黑白棋分布
- 还原选中备份：还原前自动生成 .before_restore_*.bak
- 修改当前金币：修改前自动生成 .before_coins_*.bak
- 删除当前存档或选中备份里的所有黑棋/敌方棋子：修改前自动 .bak
- 修改当前奇兵/Gambit：编辑 CurrentGambits，每行一个
- 修改库存棋子：编辑 PiecesInStock，每行一个

默认存档路径：
C:\Users\zks\AppData\LocalLow\Blukulélé\Gambonanza\save.json

默认会复用旧备份目录：
C:\Users\zks\Desktop\Gambonanza_SL\backups

建议：
还原备份或修改存档前，先退出游戏，避免 Steam/游戏进程覆盖文件。
