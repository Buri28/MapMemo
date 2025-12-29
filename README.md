# 準備中…


# Map Memo

マップごとにメモを残して表示するMODです。  
マップの特徴や課題を残して、再度プレイする時などに確認ができます。すぐ忘れてしまう人にお勧めです。  
<img width="50%" height="50%" alt="image" src="https://github.com/user-attachments/assets/fc584cb4-d1c3-4101-824f-2120c3876856" />  
更新日時が表示されるため、メモを残した日時も確認できます。   

<img width="50%" height="50%" alt="image" src="https://github.com/user-attachments/assets/e2fbc172-2857-46a4-b3aa-e7739a9eb732" />   

辞書ファイルや入力履歴から入力候補が表示されます。  

<!-- 
## 特徴
| 機能       | 概要                     | 
|------------|--------------------------|
| メモの編集 | マップ詳細画面のメモマークを押すとメモが編集ができます。   | 
| ツールチップ表示 | メモマークにカーソルを当てることでツールチップでメモを確認できます。         | 
| 入力アシスト | 入力した履歴のファイルと辞書ファイルから、キー入力時に候補が表示されます。         | 
| 入力履歴の設定 | MODSの「MAP MEMO」タブから入力履歴に関する設定が変更できます。        | 
| 絵文字の入力 | 絵文字のタブから絵文字のキーを押すことで、さらに候補となる絵文字が表示されます。        |
| キーバインディング | キーバインディング用の設定ファイルにより、表示されるキーの配置を変更できます。<br>※絵文字はキーだけでなく表示される候補の設定も可能です。        |
-->

## インストール  
Pluginsフォルダに「MapMemo.dll」を格納してBeatSaberを起動します。

## 操作方法
### マップ選択画面
  ジャケットの右下のペンマークのアイコンを押すことでメモ編集画面が開きます。  
  <img width="30%" height="30%" alt="image" src="https://github.com/user-attachments/assets/44d48c05-91ae-4b53-be06-9dd8624034a6" />  
  メモがある場合、ツールチップでメモの内容を確認できます。(ペンマークのアイコンが変わります)  
  <img width="30%" height="30%" alt="image" src="https://github.com/user-attachments/assets/9bb3369e-0828-48be-b19d-249a723b5acb" />

  
### メモ編集画面
  メモを編集し、SAVEボタンで保存します。
  ※SAVEボタンで画面は閉じない仕様のためCLOSEボタンで画面を閉じます。    
  ※メモは3行まで入力できますが、ツールチップの表示では改行はスペースになります。  
  - 「A」タブ  
    英字、数字、記号が入力できます。  
    <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/ef2d3ae8-71b4-4854-a961-d0e2a430a86f" />   

  - 「あ/ア」タブ  
    ひらがな、カタカナが入力できます。    
    <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/92ac1525-42e9-4641-af82-1e50416c0530" />
    
  - 「🙂」タブ  
    絵文字が入力できます。  
    絵文字1字を入力すると入力候補に関連する絵文字が表示されます。  
    <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/fa83e474-f3a1-486e-8464-261276546684" />


### 設定画面  
  入力履歴に関する設定を行います。  
  ※メモ編集画面の入力候補には、辞書ファイルの候補より上に入力履歴の候補が新しい順に表示されます。  
 <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/18ff7af0-0474-4d3a-97f2-75ac0cd0356a" />

| 項目       | 説明                     | 
|------------|--------------------------|
| Max History Count   | 履歴ファイルに保存する件数を設定します。0～5000(デフォルト値:1000)   | 
| History Show Count | 入力候補に表示する件数を設定します。0～10(デフォルト値:3)         | 
| Clear Historyボタン | 履歴ファイルを削除し、入力履歴をクリアします。         | 

 
## カスタマイズ
### 辞書ファイルをカスタマイズ
  「<BeatSaber>\UserData\MapMemo」フォルダの「#dictionary.txt」を編集することで入力候補に表示される単語を追加/変更できます。    
### キーバインディングをカスタマイズ
 「<BeatSaber>\UserData\MapMemo」フォルダの「#key_bindings.json」を編集することでキーバインドを変更できます。  
 (文字や絵文字の追加も可能です)    
 - 「A」タブのキーのバインド
   - keyNo:1～72 (type:Literal)  
     並び順:左上から右    
 - 「あ/ア」タブのキーバインド
   - keyNo:101～175 (type:Literal)  
     並び順:右上から下  
 - 「🙂」タブのキーバインド  
   絵文字の範囲を文字コードで複数設定することで絵文字が入力候補に表示されます。
   - keyNo:1～64 (type:Emoji)  
     並び順:左上から右  
     ※比較的新しい絵文字は使用できせまん。(ゲーム画面で?と表示されます)
   - デフォルトで表示される全絵文字一覧はこちら  
     [全絵文字リスト(v1.0.0)](docs/all_emoji_list_v1.0.0.txt)  
     ※ゲーム画面では文字コードが異なるため表示は異なります。

## 謝辞・参考文献
入力候補に表示される辞書ファイルの作成にあたり  
hibitさんの[ビーセイゆるふわ用語集](https://docs.google.com/document/d/1Zl8jh0djB80o3tbTmGR1MF9gV9pkYPfxqTC4IuRQmd0/edit?tab=t.0#heading=h.i3l4hzm490vv)を参考にさせていただきました🙇‍♂️

　　 

