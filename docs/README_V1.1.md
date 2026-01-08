マップごとにローカルメモを残すMODです。  
マップの特徴や課題を残して、再度プレイする時などに確認ができます。すぐ忘れてしまう人にお勧めです。  

<img width="50%" height="50%" alt="image" src="https://github.com/user-attachments/assets/e022f62c-b296-4dbb-aee9-f0455bb3b6f6" />  

更新日時が表示されるため、メモを残した日時も確認できます。  
設定が有効の場合、マップのBSRとRatingが表示されます。【v1.1】   
<sup>$\color{green}{\text{※BSRとRatingはBeatSaverから最後にデータ取得したときのものが表示されます。}}$</span></sup>  

<img width="50%" height="50%" alt="image" src="https://github.com/user-attachments/assets/9f549ec2-9286-4079-b6c4-b9a20e58211c" />  

辞書ファイルや入力履歴から入力候補が表示されます。  

<img width="50%" height="50%" alt="image" src="https://github.com/user-attachments/assets/9e0a38a0-7945-4e42-a541-5822f1548433" />  

「🛈」タブには、BeatSaverのマップ説明文などが表示されます。【v1.1】
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
  <img width="30%" height="30%" alt="image" src="https://github.com/user-attachments/assets/f0771c7d-d2e1-470b-ba10-b168d9d4a05f" />
  
### メモ編集画面
  メモを編集し、SAVEボタンで保存します。 
  - 仕様
    - SAVEボタンで画面は閉じません。(CLOSEボタンで画面を閉じます)  
    - 入力が0文字でSAVEボタンを押すと、メモは削除されます。  
    - メモは5～6行まで入力できますが、ツールチップの表示では改行はスペースになります。  
      【v1.1 3行から変更、スクロール対応】    
  - 「A」タブ  
    英字、数字、記号が入力できます。  
    <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/ef2d3ae8-71b4-4854-a961-d0e2a430a86f" />   

  - 「あ/ア」タブ  
    ひらがな、カタカナが入力できます。  
    <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/b9a90605-94ef-456b-8d1b-fa71545eabfa" />  
    
  - 「🙂」タブ  
    絵文字が入力できます。  
    絵文字1字を入力すると入力候補に関連する絵文字が表示されます。  
    <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/fa83e474-f3a1-486e-8464-261276546684" />

  - 「🛈」タブ【v1.1】  
    BeatSaverのマップ情報(説明文、パブリッシュ日、Rating)が表示されます。  
    「GET LATEST」ボタンを押すとBeatSaverから最新情報を取得します。    
     <sup>$\color{green}{\text{※BSRとRatingはBeatSaverから最後にデータ取得したときのものが表示されます。(設定画面のBeatSaver Access Modeを参照)}}$</sup>  
    <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/9e0a38a0-7945-4e42-a541-5822f1548433" />  

### 設定画面  
  入力履歴に関する設定を行います。  
  <sup>$\color{green}{\text{※メモ編集画面の入力候補には、辞書ファイルの候補より上に入力履歴の候補が新しい順に表示されます。}}$</sup>  
  <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/de0fb5a1-2890-4904-b891-b1a60e3f1dcc" />
  <img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/72113253-6ec6-47a9-a306-4226d604bd51" />
| 項目       | 説明                     | 
|------------|--------------------------|
| Show BSR in Tooltip | ツールチップにマップのBSR(※)を表示します。(デフォルト値:有効) 【v1.1】 |
| Show Rating in Tooltip | ツールチップにマップのRating(※)を表示します。(デフォルト値:有効) 【v1.1】 |
| Auto Create Empty Memo After Play | プレイ後に空のメモを作成します。(デフォルト値:無効) 【v1.1】 |
| BeatSaver Access Mode |いつBeatSaverにアクセスするかのモード【v1.1】<br><b><sup>(マップ選択時に最新を表示したい場合はAutoにしてください)</sup></b><br><b>Manual：② ／Semi-Auto：①②③ ／Auto：①②③④</b> (デフォルト値:Semi-Auto)<br>①メモ編集画面初回表示時 <br>②「🛈」タブのGET LATESTボタンを押した時<br> ③マッププレイ終了時<br> ④マップ選択時 |
| Max History Count   | 履歴ファイルに保存する件数を設定します。0～5000(デフォルト値:1000)   | 
| History Show Count | 入力候補に表示する件数を設定します。0～10(デフォルト値:3)         | 
| Clear Historyボタン | 履歴ファイルを削除し、入力履歴をクリアします。 |  

<sup>$\color{green}{\text{※BSRとRatingはBeatSaverから最後にデータ取得したときのものが表示されます。}}$</sup>  

### メモファイル形式
bsrCode、beatSaverUrl、autoCreateEmptyMemoを追加しています。【v1.1】  
<img width="40%" height="40%" alt="image" src="https://github.com/user-attachments/assets/676a0db7-a2a8-4f94-89fc-9d6f47bc8f34" />
 
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
     <sup>$\color{green}{\text{※比較的新しい絵文字は使用できせまん。(ゲーム画面で?と表示されます)}}$</sup>
   - デフォルトで表示される全絵文字一覧はこちら  
     [全絵文字リスト(v1.0.0)](docs/all_emoji_list_v1.0.0.txt)  
     <sup>$\color{green}{\text{※ゲーム画面ではフォントが異なるため表示は異なります。}}$</sup>

## 謝辞・参考文献
入力候補に表示される辞書ファイルの作成にあたり  
hibitさんの[ビーセイゆるふわ用語集](https://docs.google.com/document/d/1Zl8jh0djB80o3tbTmGR1MF9gV9pkYPfxqTC4IuRQmd0/edit?tab=t.0#heading=h.i3l4hzm490vv)を参考にさせていただきました🙇‍♂️

　　 

