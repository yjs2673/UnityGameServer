# UnityGameServer
Unity Game Server Project by .NET

## **🛠️ 기술 스택**

| **구분** | **기술** | **역할** |
| --- | --- | --- |
| **Framework** | **.NET 8.0 (ASP.NET Core)** | 고성능 서버 엔진 및 API 프레임워크 |
| **Language** | **C#** | 서버 로직 및 데이터 모델 작성 |
| **Database** | **MySQL** | 유저 정보 및 게임 데이터 영구 저장 |
| **ORM** | **Entity Framework Core** | C# 코드로 DB를 조작 (SQL 직접 작성 방지) |
| **Client** | **Unity (C#)** | 게임 로직 및 서버 통신(UnityWebRequest) 담당 |

---

## 🏗️ **서버 구현 방식**

**REST(Representational State Transfer)** **API** 서버

### 1. 무상태성 (Stateless)

- 서버는 클라이언트의 상태(로그인 여부 등)를 메모리에 유지 X
- 유니티가 로그인 요청하면 DB를 확인해 응답 후 연결을 끊음
- **장점:** 서버 메모리 부하가 적고, 동접자가 늘어나 서버를 증설(Scale-out)할 때 유리

### 2. HTTP 메서드 기반 통신

- **POST:** 새로운 데이터를 생성(회원가입)하거나 보안이 필요한 데이터(로그인)를 보낼 때 사용
- **GET:** 데이터를 조회할 때 사용(정보 불러오기)
- **PUT/PATCH:** 기존 데이터를 수정(골드 변경)

### 3. 데이터 형식: JSON

- 서버와 유니티는 JSON(JavaScript Object Notation)으로 대화
- `{ "LoginId": "test", "Gold": 100 }` 같은 형태라 사람이 읽기 쉽고 기기 간 호환성이 좋음

---

## 🔄 데이터 흐름 (Workflow)

1. **클라이언트 (Unity):** 유저가 입력한 데이터를 JSON으로 말아서 `UnityWebRequest`로 서버에 보낸다.
2. **서버 (ASP.NET Core):** `Controller`가 요청을 받아 `DTO(Data Transfer Object)`에 담는다.
3. **데이터베이스 (EF Core + MySQL):** 서버가 DB에 물어보거나 값을 저장한다.
4. **응답:** 서버가 결과(성공/실패)와 필요한 데이터(닉네임, 골드 등)를 다시 JSON으로 응답한다.
5. **반영:** 유니티는 받은 JSON을 파싱하여 `GameManager`(싱글톤)에 저장하고 화면을 전환한다.
