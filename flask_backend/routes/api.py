from flask import Blueprint, jsonify, request
from utils.db import db
from bson import ObjectId
import random

bp = Blueprint("api", __name__, url_prefix="/api")

# 🔹 Get all texts with their open questions
@bp.route("/all", methods=["GET"])
def get_all_question_sets():
    texts = db.texts.find()
    output = []

    for text_doc in texts:
        text_id = text_doc["_id"]
        text_content = text_doc.get("text_content", "")

        questions = list(db.questions.find({"text_id": text_id}))
        formatted_questions = []
        for q in questions:
            formatted_questions.append({
                "question": q.get("question"),
                "reference_answer": q.get("reference_answer"),
                "type": q.get("type"),
            })

        output.append({
            "id": str(text_id),
            "text": text_content,
            "questions": formatted_questions
        })

    return jsonify(output), 200

# 🔹 Get one random text with open questions (used by Unity)
@bp.route("/random-text-open-questions", methods=["GET"])
def get_random_text_with_open_questions():
    texts = list(db.texts.find())
    if not texts:
        return jsonify({"error": "No texts found"}), 404

    selected_text = random.choice(texts)
    text_id = selected_text["_id"]

    questions = list(db.questions.find({"text_id": text_id}))
    formatted_questions = [
        {
            "question": q.get("question"),
            "reference_answer": q.get("reference_answer"),
            "type": q.get("type"),
        } for q in questions
    ]

    return jsonify({
        "text": {
            "_id": str(text_id),
            "text_content": selected_text.get("text_content", "")
        },
        "questions": formatted_questions
    }), 200

# 🔹 Get all closed (multiple-choice) questions
@bp.route("/closed-questions", methods=["GET"])
def get_closed_questions():
    questions = list(db.closed_questions.find())
    output = [
        {
            "question": q.get("question"),
            "answers": q.get("answers"),  # expects a list of strings
            "correctAnswerIndex": q.get("correctAnswerIndex")
        }
        for q in questions
    ]
    return jsonify(output), 200

# 🔹 Save user answers 
@bp.route("/user-answers", methods=["POST"])
def save_user_answers():
    data = request.get_json()
    if not data or "answers" not in data:
        return jsonify({"error": "Invalid data"}), 400

    db.answers.insert_one(data)
    return jsonify({"message": "Answers saved successfully"}), 201

# 🔹 Save reading recordings in base 64 
@bp.route("/reading-recordings", methods=["POST"])
def save_reading_recording():
    data = request.get_json()
    if not data or "audio_base64" not in data:
        return jsonify({"error": "Missing audio_base64 field"}), 400

    db.reading_recordings.insert_one(data)
    return jsonify({"message": "Recording saved"}), 201
