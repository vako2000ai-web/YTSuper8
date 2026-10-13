"""Roller Coaster Heroes starter API. No actual DonationAlerts credentials or processing yet."""
from datetime import datetime, timezone
from pathlib import Path
import sqlite3
from contextlib import contextmanager
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

ROOT = Path(__file__).resolve().parent
DB = ROOT / "rollercoaster.sqlite3"
app = FastAPI(title="Roller Coaster Heroes", version="0.1.0")

@contextmanager
def connection():
    con = sqlite3.connect(DB)
    con.row_factory = sqlite3.Row
    con.execute("PRAGMA foreign_keys=ON")
    try:
        yield con
        con.commit()
    except Exception:
        con.rollback()
        raise
    finally:
        con.close()

def bootstrap():
    with connection() as con:
        con.executescript("""
        CREATE TABLE IF NOT EXISTS characters (
          id INTEGER PRIMARY KEY, name TEXT NOT NULL UNIQUE);
        CREATE TABLE IF NOT EXISTS tournaments (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          title TEXT NOT NULL, status TEXT NOT NULL DEFAULT 'created',
          created_at TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS races (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          tournament_id INTEGER NOT NULL REFERENCES tournaments(id),
          hero_id INTEGER NOT NULL REFERENCES characters(id),
          round_number INTEGER NOT NULL CHECK(round_number BETWEEN 1 AND 3),
          elapsed_seconds REAL NOT NULL CHECK(elapsed_seconds >= 0),
          track_coins INTEGER NOT NULL DEFAULT 0 CHECK(track_coins >= 0),
          bonus_coins INTEGER NOT NULL DEFAULT 0 CHECK(bonus_coins >= 0),
          support_coins INTEGER NOT NULL DEFAULT 0 CHECK(support_coins >= 0),
          score INTEGER NOT NULL CHECK(score >= 0),
          UNIQUE(tournament_id, round_number, hero_id));
        CREATE TABLE IF NOT EXISTS donations (
          provider_event_id TEXT PRIMARY KEY,
          amount_cents INTEGER NOT NULL CHECK(amount_cents > 0),
          currency TEXT NOT NULL CHECK(currency = 'USD'),
          donor TEXT, command TEXT, status TEXT NOT NULL DEFAULT 'received',
          received_at TEXT NOT NULL);
        """)
        con.executemany("INSERT OR IGNORE INTO characters(id,name) VALUES (?,?)",
            list(enumerate(["Fox","Bolt","Dino","Chick","Ziggy","Bruno","Rocky","Rico","Mia","Ping"],1)))
bootstrap()

class TournamentCreate(BaseModel):
    title: str = Field(min_length=1, max_length=150)

class RaceResult(BaseModel):
    hero_id: int = Field(ge=1, le=10)
    round_number: int = Field(ge=1, le=3)
    elapsed_seconds: float = Field(ge=0, le=3600)
    track_coins: int = Field(ge=0, le=100000)
    bonus_coins: int = Field(ge=0, le=100000)
    support_coins: int = Field(ge=0, le=100000)

@app.get("/health")
def health():
    return {"status":"ok", "mode":"development", "donations_connected":False}

@app.get("/characters")
def characters():
    with connection() as con:
        return [dict(r) for r in con.execute("SELECT * FROM characters ORDER BY id")]

@app.post("/tournaments", status_code=201)
def create_tournament(request: TournamentCreate):
    with connection() as con:
        cursor=con.execute("INSERT INTO tournaments(title,created_at) VALUES (?,?)",
            (request.title,datetime.now(timezone.utc).isoformat()))
        return {"id":cursor.lastrowid, "status":"created"}

@app.post("/tournaments/{tournament_id}/races", status_code=201)
def add_demo_race(tournament_id: int, race: RaceResult):
    """Development-only submission. Replace with authenticated server-verified events."""
    with connection() as con:
        if not con.execute("SELECT 1 FROM tournaments WHERE id=?", (tournament_id,)).fetchone():
            raise HTTPException(status_code=404, detail="Tournament not found")
        score = max(0,round(10000 - 50*race.elapsed_seconds)) + 100*(
            race.track_coins+race.bonus_coins+race.support_coins)
        try:
            cursor=con.execute("""INSERT INTO races
                (tournament_id,hero_id,round_number,elapsed_seconds,track_coins,bonus_coins,support_coins,score)
                VALUES(?,?,?,?,?,?,?,?)""", (tournament_id,race.hero_id,race.round_number,
                race.elapsed_seconds,race.track_coins,race.bonus_coins,race.support_coins,score))
        except sqlite3.IntegrityError as exc:
            raise HTTPException(status_code=409, detail="Race already exists or invalid hero") from exc
        return {"race_id":cursor.lastrowid,"score":score}

@app.get("/tournaments/{tournament_id}/leaderboard")
def leaderboard(tournament_id: int):
    with connection() as con:
        if not con.execute("SELECT 1 FROM tournaments WHERE id=?", (tournament_id,)).fetchone():
            raise HTTPException(status_code=404, detail="Tournament not found")
        return [dict(r) for r in con.execute("""
            SELECT c.id AS hero_id,c.name,COUNT(r.id) AS races,
                   COALESCE(SUM(r.score),0) AS total_score,
                   COALESCE(SUM(r.track_coins),0) AS track_coins,
                   COALESCE(SUM(r.elapsed_seconds),0) AS total_seconds
            FROM characters c LEFT JOIN races r
              ON r.hero_id=c.id AND r.tournament_id=?
            GROUP BY c.id,c.name
            ORDER BY total_score DESC, track_coins DESC, total_seconds ASC, c.id ASC
        """,(tournament_id,))]
