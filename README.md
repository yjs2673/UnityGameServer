# UnityGameServer
Unity Game Server Project by .NET

</br>

## 🛠️ 기술 스택

| **구분** | **기술** | **역할** |
| --- | --- | --- |
| **Framework** | **.NET 8.0 (ASP.NET Core)** | 서버 엔진 및 API 프레임워크 |
| **Language** | **C#** | 서버 로직 및 데이터 모델 작성 |
| **Database** | **MySQL** | 유저 정보 및 게임 데이터 영구 저장 |
| **ORM** | **Entity Framework Core** | 객체 지향적 DB 설계 및 데이터 매핑 |
| **Memory Opt.** | **Span<T>, Memory<T>** | Zero-copy 데이터 조작으로 GC 부하 최소화 |
| **Comunication** | **TCP Socket (Async)** | 비동기 소켓 기반 실시간 양방향 패킷 통신 |
| **Serialization** | **Custom Packet Marshaler** | 바이트 단위 직접 직렬화를 통한 네트워크 대역폭 최적화 |
| **Concurrency** | **Lock & ThreadLocal** | 멀티스레드 환경의 자원 공유 및 동기화 제어 |

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

- **브로드캐스팅 (Broadcasting):** 특정 유저의 이동, 애니메이션, 설정 변경 등을 접속 중인 모든 클라이언트에게 실시간으로 전파

- **패킷 직렬화 (Serialization):** Span<byte>와 BitConverter를 활용하여 데이터를 바이트 배열로 변환, 네트워크 대역폭 최적화 및 고성능 처리 구현

- **스레드 안전성 (Thread-Safe):** lock과 PacketQueue 활용하여 멀티스레드 환경에서도 데이터 유실 및 레이스 컨디션 방지

### 5. 데이터 검증 및 보안 (DTO & Validation)

- **DTO (Data Transfer Object):** 클라이언트와 서버 간 데이터 전송 전용 객체 사용으로 보안 강화

- **입력 제한:** 유효성 검사 어노테이션([StringLength])을 통해 아이디 및 닉네임의 글자 수(10자 제한) 등을 서버 사이드에서 강제

- **에러 핸들링:** ModelState를 활용하여 규격에 맞지 않는 요청 발생 시 클라이언트에 명확한 한글 에러 메시지 반환

---

</br>

## 💬 실시간 채팅 시스템

**1. 세션 매핑 (Session Mapping)**

- HTTP 로그인 시 발급된 User ID를 소켓 연결 시점에 서버 메모리(Dictionary)에 매핑하여 유저 식별

- 접속 시 해당 유저의 닉네임을 서버가 보유하여 시스템 메시지(입장, 퇴장 알림) 등에 활용

**2. 클라이언트 큐 시스템 (Message Queue)**

- **네트워크 스레드 분리:** 유니티의 메인 스레드 정지(Freezing)를 방지하기 위해 별도 스레드에서 메시지 수신

- **메인 스레드 동기화:** 수신된 메시지를 큐(Queue)에 쌓아두고 유니티의 Update() 루프에서 채팅 UI에 반영

**3. 시스템 이벤트 알림**

- **입장/퇴장 감지:** 유저의 소켓 접속 및 연결 해제(로그아웃 포함) 시 실시간으로 전체 공지 메시지 송출

---

</br>

## 🎮 실시간 동기화 시스템

**1. 위치 및 회전 동기화 (Transform Sync)**

- **실시간 좌표 공유:** 유니티의 메인 스레드 정지(Freezing)를 방지하기 위해 별도 스레드에서 메시지 수신

- **원격 플레이어 관리:** PlayerManager를 통해 접속 중인 타 유저를 RemotePlayer 프리팹으로 동적 생성 및 파괴

**2. 애니메이션 상태 동기화 (Animation Sync)**

- **상태 기반 동기화 (State-based):** 애니메이션 제어 파라미터를 패킷에 포함하여 이동 상태 공유

- **트리거 동기화 (Trigger Sync):** 점프, 구르기 등의 단발성 액션을 이전 상태값 비교 로직을 통해 타 클라이언트에서도 동일한 타이밍에 재생

**3. 안정적인 소켓 관리**

- **안전한 종료 (Graceful Shutdown):** 씬 이동이나 앱 종료 시 `CloseSocket()` 호출을 통해 서버에 퇴장 알림(S_Leave)을 명시적으로 전달

- **예외 처리:** 소켓 연결 유무를 상시 체크하여 SocketException으로 인한 클라이언트 크래시 방지

---

</br>

## 🔄 데이터 흐름 (Workflow)

1. **클라이언트 (Unity):** 유저가 입력한 데이터를 JSON 형식으로 `UnityWebRequest`로 서버에 전송
2. **서버 (ASP.NET Core):** `Controller`가 요청을 받아 `DTO`에 저장하고 DB(MySQL)와 통신
3. **데이터베이스 (EF Core + MySQL):** 서버가 DB에 물어보거나 값을 저장
4. **응답:** 서버가 결과와 필요한 데이터를 JSON으로 응답 후 유니티는 이를 파싱하여 UserManager에 저장
5. **소켓 연결:** 로비(Lobby) 또는 공원(Park) 씬 진입 시 서버의 `TCP 포트`로 접속 시도 및 `세션 ID` 할당
6. **패킷 송수신 (Sync):** Update() 루프에서 자신의 Transform과 Animator 상태를 `C_Move 패킷`으로 전송
7. **브로드캐스팅 (Broadcasting):** 서버는 수신된 패킷을 `S_Move 패킷`으로 나를 제외한 모든 접속자에게 전송
8. **원격 반영:**
  - Lobby: 유저가 메시지 전송 시 서버가 이를 수신하여 모든 접속자에게 복사 전달
  - Park: 패킷을 받은 타 클라이언트들은 해당 ID의 RemotePlayer 오브젝트를 찾아 위치와 애니메이션 갱신
9. **연결 해제:**
  - Lobby: 로그아웃 또는 앱 종료 시 소켓을 닫고 서버 리소스를 해제하고 퇴장 알림 처리
  - Park: 로그아웃 또는 앱 종료 시 소켓을 닫고 서버는 해당 세션을 제거한 후 다른 유저들에게 `S_Leave 패킷`을 전송하여 캐릭터 파괴

---

</br>

## 🛠️ 트러블슈팅 및 최적화 (Optimization)

**1. Redis 세션 키 중복 생성 및 누수 해결**
- **문제**: 씬 이동(Lobby ↔ Park) 시마다 새로운 Redis 세션이 생성되어 로그아웃 시 잔여 데이터가 남는 현상
- **해결**: 유저의 고유 식별자(UserSeq)를 Redis Key의 고정 필드로 사용하도록 로직 변경, 기존 세션 존재 시 생성 대신 업데이트(Overwrite)를 수행하여 메모리 누수 방지 및 세션 일관성 확보

**2. 위치 동기화 시 튀는 현상(Jittering) 개선**
- **문제**: 타 유저 혹은 자신의 캐릭터가 간헐적으로 이전 좌표로 순간이동하는 현상
- **해결**: 서버로부터 수신된 본인 캐릭터의 위치 패킷은 무시하도록 PacketHandler 로직 수정 (Client-side Prediction)