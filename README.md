# 💣 Goodbye F**king Files 💥

## 📝 概要

**Windowsで削除不可能なファイルやディレクトリを、ありとあらゆる手段を用いて強制的に削除するアプリ**です。

以下のような機能が含まれています:
- 🔍 ファイルを使用しているプロセスを特定し、自動でファイルハンドラーを解放
- 💀 もし解放されない場合、プロセスを特権「SeDebugPrivilege」で強制終了
- 🔐 ユーザー権限のみの環境でも、UACをバイパスして管理者権限に昇格
- 👑 管理者権限から「TrustedInstaller」特権に昇格し、セキュリティ機構を突破
- 🧼 ファイルの属性を初期化し、読み取り専用設定などを無効化
- 🛣️ DOS Device Path を利用して予約済みファイル名や長すぎるパスを削除

このツールを使えば、以下のようなファイルを削除可能になります:
- ❌ "使用中" で消せないファイル
- 🚫 アクセス拒否されるファイル
- 🛡️ TrustedInstaller 所有のファイル
- 🔓 ハンドルが開きっぱなしのファイル

## ⚙️ 利用方法

初めに、本リポジトリを複製するか、ダウンロードして展開してください。

```bash
git clone https://github.com/ActiveTK/gff
cd gff
```

🧑‍💻 実行には**管理者権限**が必要です。  
エクスプローラで「管理者として実行」するか、管理者権限付きのコマンドプロンプトで起動してください。

削除したいファイルまたはディレクトリのパスを引数に指定するだけで、強制削除できます:

```bash
gff "削除したいファイルまたはディレクトリのパス"
```

または、`gff.exe` に削除対象ファイルをドラッグ＆ドロップするだけでも動作します。

## ⚠️ 利用上の注意

このツールは、**原則としてすべてのファイルを削除可能**です。
**誤操作はシステムを破壊しかねません**ので、十分ご注意ください。

- 🗑️ ファイル削除時に1回、ディレクトリ削除時に2回の確認プロンプトが表示されます。
- 🎯 パス指定に間違いがないか、必ず確認してください。

## 🚫 免責事項

このツールは、Windowsで削除困難なファイルを対象に、  
**高度なAPIや特権操作で削除を補助する目的**で作成されています 🛠️

ご利用前に、以下に同意してください:

- 🖥️ **自己所有のPC環境でのみ使用してください**
- 🏢 **企業・団体・学校などの監視下での使用は非推奨**
- 💥 **本ツールの使用による損害・データ損失に制作者は一切責任を負いません**
- 🧬 **マルウェア目的での再配布・改変・組み込みは禁止**

🎓 本ツールはセキュリティ研究・学習・個人利用のために提供されています。  
⚖️ MITライセンスの下で公開されていますが、**悪用は法律で厳しく罰せられる可能性があります**。

## 📄 ライセンス

このプログラムは The MIT License の下で公開されています。

© 2025 ActiveTK.  
🔗 https://github.com/ActiveTK/gff/blob/master/LICENSE

## 🛠️ 内部設定

以下の設定項目は、先頭の「#」を外すことで有効になります 🧩:

```
# ShowDebugMessages
# AllowDangerOperation
```
