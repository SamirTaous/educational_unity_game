from flask import Blueprint, request, jsonify, session
from utils.db import db
from werkzeug.security import generate_password_hash, check_password_hash

bp = Blueprint("auth", __name__, url_prefix="/auth")

@bp.route("/register", methods=["POST"])
def register():
    data = request.get_json()
    username = data.get("username")
    password = data.get("password")
    role = data.get("role")
    level = data.get("level")  # only for students

    if not username or not password or not role:
        return jsonify({"error": "Missing required fields"}), 400

    if db.students.find_one({"username": username}) or db.teachers.find_one({"username": username}):
        return jsonify({"error": "Username already exists"}), 409
    
    if role not in ["student", "teacher"]:
        return jsonify({"error": "Invalid role"}), 400

    hashed = generate_password_hash(password)

    if role == "student":
        if not level:
            return jsonify({"error": "Level is required for students"}), 400
        db.students.insert_one({
            "username": username,
            "password_hash": hashed,
            "level": level,
            "role": "student"
        })
    else:
        db.teachers.insert_one({
            "username": username,
            "password_hash": hashed,
            "role": "teacher"
        })

    return jsonify({"message": "User registered successfully."}), 201



@bp.route("/login", methods=["POST"])
def login():
    data = request.get_json()
    username = data.get("username")
    password = data.get("password")

    if not username or not password:
        return jsonify({"error": "Username and password required"}), 400

    # Try to find user in both collections
    user = db.students.find_one({"username": username})
    role = "student"
    if not user:
        user = db.teachers.find_one({"username": username})
        role = "teacher"

    if not user or not check_password_hash(user["password_hash"], password):
        return jsonify({"error": "Invalid credentials"}), 401

    # Set session data
    session["user_id"] = str(user["_id"])
    session["username"] = user["username"]
    session["role"] = role
    if role == "student":
        session["level"] = user["level"]
    else:
        session.pop("level", None)  # Remove stale level data if previously logged in as student

    # Build response
    response = {
        "message": "Login successful",
        "username": user["username"],
        "role": role,
        "user_id": str(user["_id"])
    }
    if role == "student":
        response["level"] = user["level"]

    return jsonify(response), 200




@bp.route("/logout", methods=["POST"])
def logout():
    session.clear()
    return jsonify({"message": "Logged out"}), 200


@bp.route("/session", methods=["GET"])
def get_current_user():
    if "user_id" in session:
        response = {
            "user_id": session["user_id"],
            "username": session["username"],
            "role": session["role"]
        }
        if session["role"] == "student":
            response["level"] = session.get("level")
        return jsonify(response), 200

    return jsonify({"error": "No active session"}), 401
