from flask import Blueprint, request, jsonify, session
from werkzeug.security import generate_password_hash, check_password_hash
from utils.db import db

bp = Blueprint("auth", __name__)

@bp.route("/register", methods=["POST"])
def register():
    data = request.json
    if db.users.find_one({"username": data["username"]}):
        return jsonify({"error": "User already exists"}), 400

    user = {
        "username": data["username"],
        "password": generate_password_hash(data["password"]),
        "role": data["role"]  # "student" or "teacher"
    }
    db.users.insert_one(user)
    return jsonify({"message": "User registered"}), 201

@bp.route("/login", methods=["POST"])
def login():
    data = request.json
    user = db.users.find_one({"username": data["username"]})
    if user and check_password_hash(user["password"], data["password"]):
        session["user_id"] = str(user["_id"])
        session["role"] = user["role"]
        return jsonify({"message": "Login successful", "role": user["role"]}), 200
    return jsonify({"error": "Invalid credentials"}), 401
