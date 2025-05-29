# 사내 메신저 만들기
<br/>

#### 👨🏻‍👩🏻‍👧🏻‍👦🏻 개인 프로젝트  
---  
<br/>


  
### 📢 소개
---
+ 사내에서 사용할 만한 메신저를 만드는게 목표이며, 과연 마칠 수 있을까..
<br/><br/><br/>

### 🛠️ 개발환경
---
+ C++
+ C#
+ Windows, Linux / Ubuntu Server
+ VisualStudio, GitHub 
<br/><br/><br/>



### 💡목표
---
+ 사내에서 사용가능 할 정도로 원활한 채팅 프로그램제작
+ Ubuntu 에서 서버가동
  
<br/><br/><br/>


### ⚡기능
---
+ 250509 Ubuntu 에서 서버를 열어 사용자 구분없이 채팅가능
+ 250509 ASP.NET CORE 이용 로그인, 회원가입 인증기능 추가
+ 250512 프로필명 부서+이름+직급으로 나타내도록 수정, ~~서버수정및 클라수정 필요해보임 일부메시지 누락~~
+ 250513 서버 수정, await로 비동기 연결 및 Nagle 비활성화로 일부메시지 누락 수정완료
+ 250513 회원가입한 사내 이용자 보여주기 및 실시간 접속중 확인기능 추가
+ 250514 1:1 대화보내기 및 그룹채팅 만들고 참여하기 기능추가, ~~현재 제대로 메시지 소통이 되지않음 + 대화보내기를 했을때 상대방 채팅방에 온 메시지가 뜨지않음~~
+ 250514 ~~메시지 실시간 송수신만 구현되어있어, 이전에 왔던 메시지는 보이지않고 채팅방을 재입장하면 메시지가 날아가는 문제~~
+ ~~-> 서버 메모리에 로그를 저장할지 DB에 저장할지 선택해야함 서버가 꺼지면 로그가 전부 유실되니 DB로 하는게 맞아보임~~
+ 250514 chat_messages 테이블을 만들어 이전에 나눈 대화도 보이도록 변경, ~~메시지 발신시 연달아 보이는 문제 -> 과거메시지 실시간 메시지 두번 보여지는거 같음 서버쪽에서 수정하면 될듯함~~
+ 디자인 다듬으면 될듯한데 기능은구현했으니 여기서 마무리
  
  <br/><br/><br/>

### 🖥️ 결과물

![Image](https://github.com/user-attachments/assets/c238ed06-aed6-4d90-9e7f-63570f1ace02)

![Image](https://github.com/user-attachments/assets/9343132c-ad67-4adb-9668-db5cc02fcc2f)

![Image](https://github.com/user-attachments/assets/88b3eb6d-1aa3-4c31-a4c0-963cb3ca4c04)

![Image](https://github.com/user-attachments/assets/01046b89-f71c-4401-abaa-86241cddaea4)

![Image](https://github.com/user-attachments/assets/3573147e-eb98-4a7e-8bf1-2143d71d4482)

![Image](https://github.com/user-attachments/assets/aa54bf8c-b7a6-44e7-b94c-617b23a0630b)

![Image](https://github.com/user-attachments/assets/8adeb672-ed9c-4392-ab39-1724c6811330)

![Image](https://github.com/user-attachments/assets/60fa42d3-956b-4414-b139-f1397089df78)

![Image](https://github.com/user-attachments/assets/be296a07-39bb-432e-ac9f-cf35ff3a8657)
