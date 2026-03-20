# UnityGameServer
Unity Game Server Project by .NET

</br>

## 🛠️ 기술 스택

| **구분** | **기술** | **역할** |
| --- | --- | --- |
| **Framework** | **.NET 8.0 (ASP.NET Core)** | 고성능 서버 엔진 및 API 프레임워크 |
| **Language** | **C#** | 서버 로직 및 데이터 모델 작성 |
| **Database** | **MySQL** | 유저 정보 및 게임 데이터 영구 저장 |
| **ORM** | **Entity Framework Core** | C# 코드로 DB를 조작 (SQL 직접 작성 방지) |
| **Client** | **Unity (C#)** | 게임 로직 및 서버 통신(UnityWebRequest) 담당 |
| **Comunication** | **TCP Socket** | 실시간 양방향 데이터 통신 (채팅 시스템) |
| **Threading** | **Multi-threading / Async** | 비동기 소켓 리스너 및 클라이언트 수신 스레드 관리 |

---

</br>

## 🏗️ 서버 구현 방식

**REST(Representational State Transfer)** **API** 서버

### 1. 무상태성 (Stateless)

- 서버는 클라이언트의 상태(로그인 여부 등)를 메모리에 유지 X
- 유니티가 로그인 요청하면 DB를 확인해 응답 후 연결을 끊음

### 2. HTTP 메서드 기반 통신

- **POST:** 새로운 데이터를 생성(회원가입)하거나 보안이 필요한 데이터(로그인)를 보낼 때 사용
- **GET:** 데이터를 조회할 때 사용(정보 불러오기)
- **PUT/PATCH:** 기존 데이터를 수정(골드 변경)

### 3. 데이터 형식: JSON

- 서버와 유니티는 JSON(JavaScript Object Notation)으로 대화

### 4. 실시간 양방향 통신 (TCP Socket)

- **상태 유지 (Stateful):** REST API와 달리 서버와 클라이언트가 연결을 유지하여 즉각적인 데이터 전송 가능

- **브로드캐스팅 (Broadcasting):** 특정 유저가 보낸 메시지를 접속 중인 모든 클라이언트에게 실시간으로 전달

- **스레드 안전성 (Thread-Safe):** lock과 Queue를 활용하여 멀티스레드 환경에서도 데이터 유실 및 충돌 방지

### 5. 데이터 검증 및 보안 (DTO & Validation)

- **DTO (Data Transfer Object)**: 클라이언트와 서버 간 데이터 전송 전용 객체 사용으로 보안 강화

- **입력 제한**: 유효성 검사 어노테이션([StringLength])을 통해 아이디 및 닉네임의 글자 수(10자 제한) 등을 서버 사이드에서 강제

- **에러 핸들링**: ModelState를 활용하여 규격에 맞지 않는 요청 발생 시 클라이언트에 명확한 한글 에러 메시지 반환

---

</br>

## 💬 실시간 채팅 시스템

**1. 세션 매핑 (Session Mapping)**

- HTTP 로그인 시 발급된 User ID를 소켓 연결 시점에 서버 메모리(Dictionary)에 매핑하여 유저 식별

- 접속 시 해당 유저의 닉네임을 서버가 보유하여 시스템 메시지(입/퇴장 알림) 등에 활용

**2. 클라이언트 큐 시스템 (Message Queue)**

- **네트워크 스레드 분리**: 유니티의 메인 스레드 정지(Freezing)를 방지하기 위해 별도 스레드에서 메시지 수신

- **메인 스레드 동기화**: 수신된 메시지를 큐(Queue)에 쌓아두고 유니티의 Update() 루프에서 UI(Text/ScrollRect)에 반영

**3. 시스템 이벤트 알림**

- **입장/퇴장 감지**: 유저의 소켓 접속 및 연결 해제(로그아웃 포함) 시 실시간으로 전체 공지 메시지 송출

---

</br>

## 🔄 데이터 흐름 (Workflow)

1. **클라이언트 (Unity):** 유저가 입력한 데이터를 JSON으로 말아서 `UnityWebRequest`로 서버에 전송
2. **서버 (ASP.NET Core):** `Controller`가 요청을 받아 `DTO(Data Transfer Object)`에 저장
3. **데이터베이스 (EF Core + MySQL):** 서버가 DB에 물어보거나 값을 저장
4. **응답:** 서버가 결과(성공/실패)와 필요한 데이터(닉네임, 골드 등)를 다시 JSON으로 응답
5. **반영:** 유니티는 받은 JSON을 파싱하여 `UserManager`(싱글톤)에 저장하고 화면을 전환
6. **소켓 연결**: 유니티 로비 진입 시 서버의 TCP 포트(7777)로 접속 시도
7. **본인 인증 (Identity)**: 접속 직후 유저 ID와 닉네임을 전송하여 서버 세션 리스트에 등록
8. **채팅 송수신**: 유저가 메시지 전송 시 서버가 이를 수신하여 모든 접속자에게 복사 전달
9. **연결 해제**: 로그아웃 또는 앱 종료 시 소켓을 명시적으로 닫아 서버 리소스를 해제하고 퇴장 알림 처리
