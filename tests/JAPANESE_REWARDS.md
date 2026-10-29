# 日本語報酬認識（初期実装）

対象は日本語版の亀裂報酬。メニュー翻訳、インベントリ/SnapIt、2丁武器必要数は対象外。
設定のOCR言語で「日本語（報酬認識）」を選択する。初回は公式日本語LSTMモデルを取得する。

## 方式と対応範囲

`ja`設定をTesseractモデル`jpn`に明示的に対応付ける。
モデルは tesseract-ocr/tessdata_fast の commit
`87416418657359cb625c412a48b6e1d6d41c29bd` に固定。
SHA-256: `1f5de9236d2e85f5fdf4b3c500f2d4926f8d9449f28f5394472d9e8d83b91b4d`。
配布先: https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/87416418657359cb625c412a48b6e1d6d41c29bd/jpn.traineddata
ライセンス: Apache-2.0（docs/licenses/tessdata-fast-Apache-2.0.txt）。
検証済みファイルだけを`%APPDATA%\WFInfo\tessdata\jpn.traineddata`へ配置する。
破損・取得失敗時はエラーとし、未検証データでOCRを開始しない。

報酬専用の入口は `Data.GetRewardPartName` と `OCR.GetRewardTextFromImage`。
英語の既存データから、英字固有名を保った日本語の部品別名を生成する。
対応する部品: Blueprint（の設計図）、Barrel、Receiver、Stock、Handle、Upper/Lower Limb、Neuroptics、Chassis、およびForma Blueprint。
これらは提供画像で確認した表記。未対応部品を推測で別アイテムへ変換しない。
日本語WFM APIには依存しないが、既存英語アイテム一覧・価格データの取得/キャッシュは必要。
NFKCと空白/改行除去で比較し、短い名前は完全一致、10文字以上は編集距離1まで。
同点の異なるアイテムは未認識。名前全体の50%を許容する広い推測は使用しない。
古いJapaneseLanguageProcessorの重み付き距離は尺度が異なるため報酬には接続していない。

報酬画像だけ3倍拡大・白余白追加後、SingleBlock/SingleColumnを比較する。
辞書に対応できる読み取りを優先し、同距離で異なるアイテムに対応する場合は拒否。
既存の自動画面切り出しは維持。Windowsの実パイプライン検証は別途必要。

## 実画像

提供ブランチ japanese-reward、commit `e9abca4f00b461c52b3edc685104742d4059a3be` の6枚を改変せず使用。
`data/japanese_reward_001.png`〜`006.png`。
1920x1080 / Vitruvian / HDRオフ / メニュースケール「フル」。
数値倍率は未確認なのでJSONのscaling=0（指定なし）。「フル」を100%と仮定しない。
正解JSONは目視転記。displayed_textは改行を含むゲーム表記、partsは既存英語キー。
23報酬枠、6シナリオ。画像ハッシュを各JSONに記録。
元の tests/map.json（画像欠落のサンプル3件）は変更していない。

## クラウドで実行できる検証

リポジトリ直下から:

```sh
dotnet run --project tests/JapaneseRewardNames.Tests -- tests
dotnet build WFInfo.sln -c Release -p:EnableWindowsTargeting=true
```

LinuxのRelease ZIP作成には`powershell`コマンドが必要。準備済み環境では
`source /workspace/wfinfo-tools/activate.sh`の後に実行する。
純粋テストは本番の照合・モデル管理ソースをリンクして実行する。ネットワーク不要。
取得済みモデルのパスを第2引数に渡すとハッシュ/置換/再利用も検証する。
.NET SDK 10がテストに必要。WFInfo本体は引き続きnet48。
**文字列テストはOCR画像テストではない。**

## Windowsで必要な検証

.NET Framework 4.8とビルド用.NET SDK、VC++ x86/x64ランタイムを用意し:

```powershell
powershell -File tests/run_japanese_rewards.ps1
```

スクリプトはReleaseビルド、実WFInfo.exe、6シナリオの結果とexit codeを確認する。
未知の画像を成功扱いしない。ネットワーク初期化に失敗した場合もテスト未完了として扱う。
必要な外部ホスト: api.warframe.market、api.warframestat.us、raw.githubusercontent.com（既存データ処理が別のフォールバック先へアクセスする場合はログを確認）。
現クラウドでは両APIはプロキシ403で、Windows実行環境もない。6件の画像OCR合格は未確認。
Windows実機で自動切り出し、3/4候補、折返し、既存英語の回帰、GUI表示も確認する。
保存画像テスト成功と実ゲームのキャプチャ/ホットキー成功は別に報告する。

## この環境での実施結果（2026-10-29）

- 本番ソースをリンクした文字列/モデル管理テスト: 53チェック成功（23枠の目視転記を含む）。
- net48 Releaseクロスビルド成功。既存のCostura.Fody IncludeAssets警告1件。
- 6枚の原本SHA-256一致、ZIP整合性と内包exe一致、PowerShellランナー構文確認: 成功。
- Windows実アプリ/自動切り出し/画像回帰: 未実行。
- Linux Tesseract 5.5.0で手動指定の報酬領域を色抽出・3倍拡大し、jpnのPSM6/4を比較した補助実験:
  サンプルに現れる候補だけの辞書で17/23枠が正解、6枠が未認識、誤対応0。
  候補集合も前処理もWindows本番と同一ではないため、これを本番の認識率として扱わない。
  未認識: 002 Sarofang Handle / Akbronco Blueprint、003 Burston Blueprint、
  005 Yareli Neuroptics / Bronco Receiver、006 Aksomati Blueprint。
  改行末尾の読み落としや英字の誤読が残る。追加のengモデル併用やtessdata_bestへの単純置換でも一律改善しなかったため、初期版は固定fastモデルを維持した。

次はWindowsで6枚の実パイプラインを実行し、出力されたPartBox画像とOCR原文から
切り出し/改行分割を調整する。拒否閾値を広げて未認識を無理に埋めない。
この初期版は全面的な日本語報酬対応の完成版ではない。未対応部品の追加にも実表記の確認が必要。

### Windows初回実行と診断収集

提供されたWindows実行結果は6件実行、PassedTests=0、ErrorTests=0、
23枠中12枠が一致、11枠が欠落、ExtraParts=0。シナリオ平均正解率は52.78%。
5枚目は全枠未認識。文字先頭の欠落もあり、切り出し画像で原因を確認する必要がある。
認識閾値はこの結果だけでは変更していない。

ランナーは結果JSONの横に `*-diagnostics.zip` を作る。
ZIPには結果JSONと今回の各シナリオに対応する切り出しPNGのみを入れる。
テストごとに一意のDebugImageIdを付け、従来の同名PNG上書きを防止した。
結果JSONのDebugImageIdとファイル名で対応を確認できる。
ログの日本語OCR文字列の改行は `\r` / `\n` にエスケープするため、1行ずつ抽出しても欠落しない。
診断出力の変更であり、認識精度改善を確認したものではない。

### Windows診断画像に基づく倍率再試行

提供された診断ZIPでは001〜004/006の自動切り出し幅が900px、005は619pxだった。
実画像の4枠領域は約968pxで、前者は名前の先頭を切り、後者は文字より下の選択バーを読んでいた。
日本語だけ、従来の自動推定に加えEE.cfgから得られる倍率（0.5〜1.0）、
取得できない場合は1.0で再試行する。これは「フル」の文字列を無条件に100%とみなすものではなく、
別候補の読み取りを比較する処理。数値倍率や正解アイテムはテストから注入していない。
認識できた枠数が多い結果を選び、同数なら編集距離の合計が小さい結果を選ぶ。
同じ品質なら元の結果を維持し、複数の倍率から別々の枠をつぎはぎしない。
追加候補が例外になっても元の結果を維持する。照合閾値は変更しない。
これは全ての倍率/解像度に対する新しい自動検出器ではない。

本番と画像テストが同じReadRewardScreenを通る。Debug PNGにはauto/alternateを付け両方保存する。
診断の枠描画はコピーに行い、再試行に使う入力画像へ書き込まない。
59件の文字列/モデル管理/候補選択チェックとReleaseビルドを確認。
この修正後のWindows画像テスト結果はまだ得られていない。
