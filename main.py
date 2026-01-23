import pygame
import math
import sys
import random
from typing import List, Tuple, Dict, Optional, Any
from enum import Enum
from dataclasses import dataclass

# Константы
WIDTH, HEIGHT = 1400, 700
FPS = 60
CAR_LENGTH = 80
CAR_WIDTH = 40
SENSOR_RANGE = 400


# Фазы парковки
class ParkingPhase(Enum):
    SEARCHING = "searching"
    APPROACH = "approach"
    POSITIONING = "positioning"
    PREPARE_REVERSE = "prepare_reverse"
    REVERSE_RIGHT = "reverse_right"
    REVERSE_LEFT = "reverse_left"
    FINAL_ADJUST = "final_adjust"
    PARKED = "parked"
    ABORTED = "aborted"


# Нечеткая логика
class MembershipFunction:
    def calculate(self, x: float) -> float:
        raise NotImplementedError


class TriangleMF(MembershipFunction):
    def __init__(self, a: float, b: float, c: float):
        self.a, self.b, self.c = a, b, c

    def calculate(self, x: float) -> float:
        return max(0.0, min((x - self.a) / (self.b - self.a + 1e-9),
                            (self.c - x) / (self.c - self.b + 1e-9)))


class TrapezoidMF(MembershipFunction):
    def __init__(self, a: float, b: float, c: float, d: float):
        self.a, self.b, self.c, self.d = a, b, c, d

    def calculate(self, x: float) -> float:
        return max(0.0, min((x - self.a) / (self.b - self.a + 1e-9),
                            1.0, (self.d - x) / (self.d - self.c + 1e-9)))


class FuzzyVariable:
    def __init__(self, name: str, min_val: float, max_val: float):
        self.name = name
        self.min_val = min_val
        self.max_val = max_val
        self.sets: Dict[str, MembershipFunction] = {}
        self.value = 0.0

    def add_set(self, name: str, mf: MembershipFunction):
        self.sets[name] = mf

    def fuzzify(self, value: float):
        self.value = max(self.min_val, min(self.max_val, value))
        return {name: mf.calculate(self.value) for name, mf in self.sets.items()}


class FuzzyRule:
    def __init__(self, conditions: List[Tuple[FuzzyVariable, str]],
                 output: Tuple[FuzzyVariable, str], weight: float = 1.0):
        self.conditions = conditions
        self.output_var, self.output_set = output
        self.weight = weight

    def evaluate(self, inputs: Dict[str, Dict[str, float]]) -> float:
        strength = 1.0
        for var, set_name in self.conditions:
            strength = min(strength, inputs[var.name].get(set_name, 0.0))
        return strength * self.weight


class FuzzySystem:
    def __init__(self):
        self.inputs: Dict[str, FuzzyVariable] = {}
        self.outputs: Dict[str, FuzzyVariable] = {}
        self.rules: List[FuzzyRule] = []

    def add_input(self, var: FuzzyVariable):
        self.inputs[var.name] = var

    def add_output(self, var: FuzzyVariable):
        self.outputs[var.name] = var

    def add_rule(self, rule: FuzzyRule):
        self.rules.append(rule)

    def compute(self, input_values: Dict[str, float]) -> Dict[str, float]:
        fuzzified_inputs = {}
        for name, var in self.inputs.items():
            val = input_values.get(name, 0.0)
            fuzzified_inputs[name] = var.fuzzify(val)

        output_aggregations = {out_name: {} for out_name in self.outputs}

        for rule in self.rules:
            strength = rule.evaluate(fuzzified_inputs)
            if strength > 0:
                out_name = rule.output_var.name
                set_name = rule.output_set
                current = output_aggregations[out_name].get(set_name, 0.0)
                output_aggregations[out_name][set_name] = max(current, strength)

        results = {}
        for name, var in self.outputs.items():
            numerator = 0.0
            denominator = 0.0
            steps = 30
            step_size = (var.max_val - var.min_val) / steps

            for i in range(steps + 1):
                x = var.min_val + i * step_size
                mu_max = 0.0
                for set_name, strength in output_aggregations[name].items():
                    mf_val = var.sets[set_name].calculate(x)
                    mu_max = max(mu_max, min(strength, mf_val))

                numerator += x * mu_max
                denominator += mu_max

            results[name] = numerator / denominator if denominator > 0 else 0.0

        return results


class Car:
    def __init__(self, x, y, angle=0, color=(0, 100, 255)):
        self.x = x
        self.y = y
        self.angle = angle
        self.speed = 0.0
        self.max_speed = 2
        self.steering = 0.0
        self.color = color
        self.turning_radius = CAR_LENGTH * 2.5
        self.sensors = [SENSOR_RANGE] * 8
        self.previous_speed = 0.0
        self.acceleration = 0.02
        self.deceleration = 0.02
        self.length = CAR_LENGTH
        self.width = CAR_WIDTH

        # Задние датчики (добавлены для более точной проверки задней части)
        self.rear_sensors = [SENSOR_RANGE] * 3

    def update(self, dt):
        # Плавное изменение скорости
        speed_diff = self.speed - self.previous_speed

        if speed_diff != 0:
            is_accelerating = abs(self.speed) > abs(self.previous_speed)

            if is_accelerating:
                max_change = self.acceleration * dt * 60
            else:
                max_change = self.deceleration * dt * 60

            if abs(speed_diff) > max_change:
                if speed_diff > 0:
                    self.speed = self.previous_speed + max_change
                else:
                    self.speed = self.previous_speed - max_change

        self.previous_speed = self.speed

        # Обновление угла
        if abs(self.steering) > 0.1 and abs(self.speed) > 0.1:
            turning_circle = self.turning_radius / max(0.1, abs(self.steering) / 30.0)
            angular_speed = self.speed / turning_circle

            if self.steering < 0:
                angular_speed = -angular_speed

            self.angle += math.degrees(angular_speed) * dt * 60

        # Обновление позиции
        dx = self.speed * math.cos(math.radians(self.angle)) * dt * 100
        dy = self.speed * math.sin(math.radians(self.angle)) * dt * 100
        self.x += dx
        self.y += dy

    def get_corners(self):
        c, s = math.cos(math.radians(self.angle)), math.sin(math.radians(self.angle))
        dx, dy = self.length / 2, self.width / 2
        return [
            (self.x + dx * c - dy * s, self.y + dx * s + dy * c),
            (self.x - dx * c - dy * s, self.y - dx * s + dy * c),
            (self.x - dx * c + dy * s, self.y - dx * s - dy * c),
            (self.x + dx * c + dy * s, self.y + dx * s - dy * c)
        ]

    def get_bounding_box(self):
        """Получить ограничивающий прямоугольник машины"""
        corners = self.get_corners()
        x_vals = [c[0] for c in corners]
        y_vals = [c[1] for c in corners]
        return (min(x_vals), min(y_vals), max(x_vals) - min(x_vals), max(y_vals) - min(y_vals))

    def check_collision(self, other_car, margin=0):
        """Точная проверка столкновения между двумя машинами"""
        # Быстрая проверка ограничивающих прямоугольников
        rect1 = self.get_bounding_box()
        rect2 = other_car.get_bounding_box()

        # Если ограничивающие прямоугольники не пересекаются, то и полигоны не пересекаются
        if (rect1[0] > rect2[0] + rect2[2] or
                rect2[0] > rect1[0] + rect1[2] or
                rect1[1] > rect2[1] + rect2[3] or
                rect2[1] > rect1[1] + rect1[3]):
            return False

        # Если ограничивающие прямоугольники пересекаются, делаем точную проверку полигонов
        return self._polygons_intersect(self.get_corners(), other_car.get_corners(), margin)

    def _polygons_intersect(self, poly1, poly2, margin=0):
        """
        Точная проверка пересечения двух выпуклых полигонов методом SAT
        margin - минимальное расстояние между полигонами
        """

        def get_edges(poly):
            """Получить все рёбра полигона"""
            edges = []
            for i in range(len(poly)):
                p1 = poly[i]
                p2 = poly[(i + 1) % len(poly)]
                edges.append((p2[0] - p1[0], p2[1] - p1[1]))
            return edges

        def get_normals(edges):
            """Получить нормали ко всем рёбрам"""
            normals = []
            for edge in edges:
                # Нормаль перпендикулярна ребру
                normal = (-edge[1], edge[0])
                # Нормализуем
                length = math.sqrt(normal[0] ** 2 + normal[1] ** 2)
                if length > 0:
                    normals.append((normal[0] / length, normal[1] / length))
            return normals

        def project(poly, axis):
            """Спроецировать полигон на ось"""
            min_proj = float('inf')
            max_proj = -float('inf')
            for point in poly:
                proj = point[0] * axis[0] + point[1] * axis[1]
                if proj < min_proj:
                    min_proj = proj
                if proj > max_proj:
                    max_proj = proj
            return min_proj, max_proj

        # Получаем все нормали для обоих полигонов
        edges1 = get_edges(poly1)
        edges2 = get_edges(poly2)
        normals = get_normals(edges1) + get_normals(edges2)

        # Проверяем каждую ось
        for axis in normals:
            min1, max1 = project(poly1, axis)
            min2, max2 = project(poly2, axis)

            # Если проекции не пересекаются (с учётом запаса), полигоны не пересекаются
            if max1 + margin < min2 or max2 + margin < min1:
                return False

        return True

    def check_collision_with_any(self, obstacles, margin=0):
        """Проверить столкновение с любым из препятствий"""
        for obstacle in obstacles:
            if obstacle is self:
                continue
            if self.check_collision(obstacle, margin):
                return True, obstacle
        return False, None

    def check_rear_collision(self, obstacles, safety_margin=35):
        """
        Проверка столкновения задней частью
        Используется только для информации в контроллере
        """
        has_collision = False
        min_distance = float('inf')

        # Получаем полигон задней части машины
        corners = self.get_corners()
        # Задняя часть машины - это два задних угла (индексы 1 и 2)
        rear_corners = [corners[1], corners[2]]

        # Создаем небольшой полигон позади машины для проверки
        rear_polygon = []
        c, s = math.cos(math.radians(self.angle)), math.sin(math.radians(self.angle))
        half_width = self.width / 2
        half_length = self.length / 2

        # Создаем полигон позади машины
        for i in range(-5, 6):
            offset = i * half_width / 5
            point_x = self.x - half_length * c - safety_margin * c + offset * s
            point_y = self.y - half_length * s - safety_margin * s - offset * c
            rear_polygon.append((point_x, point_y))

        for obs in obstacles:
            if obs is self:
                continue

            if self._polygons_intersect(rear_polygon, obs.get_corners(), 0):
                # Вычисляем расстояние между центрами
                distance = math.hypot(self.x - obs.x, self.y - obs.y)
                if distance < min_distance:
                    min_distance = distance
                    has_collision = True

        return has_collision, min_distance

    def update_sensors(self, obstacles):
        # Основные датчики (8 шт)
        self.sensors = [SENSOR_RANGE] * 8
        angles = [-135, -90, -45, -20, 20, 45, 90, 135]

        # Задние датчики (3 шт) - отдельный массив
        self.rear_sensors = [SENSOR_RANGE] * 3
        rear_angles = [-150, -180, -210]  # Левый задний, центральный задный, правый задный

        # Обновляем основные датчики
        for i, da in enumerate(angles):
            ray_angle = math.radians(self.angle + da)
            dx, dy = math.cos(ray_angle), math.sin(ray_angle)
            min_dist = SENSOR_RANGE

            for obs in obstacles:
                corners = obs.get_corners()
                for j in range(4):
                    p1 = corners[j]
                    p2 = corners[(j + 1) % 4]

                    x1, y1 = self.x, self.y
                    x2, y2 = self.x + dx * SENSOR_RANGE, self.y + dy * SENSOR_RANGE
                    x3, y3 = p1
                    x4, y4 = p2

                    denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1)
                    if denom == 0:
                        continue

                    ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom
                    ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom

                    if 0 <= ua <= 1 and 0 <= ub <= 1:
                        dist = ua * SENSOR_RANGE
                        if dist < min_dist:
                            min_dist = dist

            self.sensors[i] = min_dist

        # Обновляем задние датчики
        for i, da in enumerate(rear_angles):
            ray_angle = math.radians(self.angle + da)
            dx, dy = math.cos(ray_angle), math.sin(ray_angle)
            min_dist = SENSOR_RANGE

            for obs in obstacles:
                corners = obs.get_corners()
                for j in range(4):
                    p1 = corners[j]
                    p2 = corners[(j + 1) % 4]

                    x1, y1 = self.x, self.y
                    x2, y2 = self.x + dx * SENSOR_RANGE, self.y + dy * SENSOR_RANGE
                    x3, y3 = p1
                    x4, y4 = p2

                    denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1)
                    if denom == 0:
                        continue

                    ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom
                    ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom

                    if 0 <= ua <= 1 and 0 <= ub <= 1:
                        dist = ua * SENSOR_RANGE
                        if dist < min_dist:
                            min_dist = dist

            self.rear_sensors[i] = min_dist

        return self.sensors

    def get_min_rear_distance(self):
        """Получить минимальное расстояние до препятствия сзади"""
        # Используем задние датчики (левые, центральный и правый)
        return min(self.rear_sensors)

    def get_very_close_rear(self):
        """Проверка на очень близкие объекты сзади (экстренная остановка)"""
        # Проверяем, есть ли объекты ближе чем 25 пикселей
        return any(dist < 25 for dist in self.rear_sensors)

    def get_side_sensor_distance(self, side="right"):
        """Получить расстояние от бокового датчика"""
        if side == "right":
            return self.sensors[5]  # Правый датчик (45°)
        else:  # left
            return self.sensors[2]  # Левый датчик (-45°)

    def get_line_alignment(self, line_y):
        """
        Проверка выравнивания с горизонтальной линией
        Возвращает минимальное расстояние от углов машины до линии и угол отклонения
        """
        corners = self.get_corners()

        # Находим углы, которые ближе всего к линии (верхние или нижние)
        # Для горизонтальной линии y = line_y
        distances = []

        for corner in corners:
            distances.append(abs(corner[1] - line_y))

        # Минимальное расстояние от любого угла до линии
        min_distance = min(distances)

        # Угол машины относительно горизонтали (0° - параллельно линии)
        angle_deviation = abs(self.angle) % 180
        if angle_deviation > 90:
            angle_deviation = 180 - angle_deviation

        return min_distance, angle_deviation

    def draw(self, screen, draw_sensors=True, show_labels=False):
        # Рисуем основную машину
        pygame.draw.polygon(screen, self.color, self.get_corners())

        # Направление автомобиля
        front = (
            self.x + 40 * math.cos(math.radians(self.angle)),
            self.y + 40 * math.sin(math.radians(self.angle))
        )
        pygame.draw.line(screen, (255, 255, 255), (self.x, self.y), front, 3)

        # Основные датчики (все синие)
        if draw_sensors and self.speed != 0:
            sensor_angles = [-135, -90, -45, -20, 20, 45, 90, 135]
            for i, da in enumerate(sensor_angles):
                ang = math.radians(self.angle + da)
                end = (
                    self.x + self.sensors[i] * math.cos(ang),
                    self.y + self.sensors[i] * math.sin(ang)
                )
                # Все датчики синие
                color = (100, 150, 255)
                pygame.draw.line(screen, color, (self.x, self.y), end, 2)

            # Задние датчики (тоже синие) - рисуются всегда если включены сенсоры
            rear_sensor_angles = [-150, -180, -210]
            for i, da in enumerate(rear_sensor_angles):
                ang = math.radians(self.angle + da)
                end = (
                    self.x + self.rear_sensors[i] * math.cos(ang),
                    self.y + self.rear_sensors[i] * math.sin(ang)
                )
                color = (100, 150, 255)
                pygame.draw.line(screen, color, (self.x, self.y), end, 3)

        # Отладочная визуализация (упрощенная версия)
        if show_labels:
            # Подписываем машину
            font = pygame.font.SysFont("Arial", 12)
            text = font.render(f"({int(self.x)}, {int(self.y)})", True, (255, 255, 255))
            screen.blit(text, (self.x - 30, self.y - 30))


@dataclass
class ParkingSpot:
    x: float
    y: float
    width: float = 110
    length: float = 140
    occupied: bool = False
    gap_size: float = 0.0  # Размер промежутка для парковки


class HybridParkingController:
    def __init__(self, empty_road=False):
        self.phase = ParkingPhase.SEARCHING
        self.target_spot = None
        self.phase_timer = 0
        self.fuzzy_system = self._build_fuzzy_system()
        self.parking_complete = False
        self.stable_timer = 0
        self.debug_info = {}
        self.recovering_from_emergency = False
        self.recovery_timer = 0

        # Храним информацию о том, пустая ли дорога
        self.empty_road = empty_road

        # Устанавливаем целевую линию в зависимости от того, пустая ли дорога
        # Для пустой дороги - линия дальше от бордюра (выше), для непустой - стандартная
        if self.empty_road:
            self.target_line_y = HEIGHT - 180  # Поднята на 30 пикселей выше для пустой дороги
        else:
            self.target_line_y = HEIGHT - 150  # Стандартная линия для дороги с машинами

        # Параметры контроля заднего хода - УВЕЛИЧЕНЫ для безопасности
        self.max_reverse_speed = 0.8  # Уменьшена максимальная скорость заднего хода
        self.safety_margin = 40  # Увеличен запас безопасности
        self.reverse_slow_distance = 90  # Увеличено расстояние для начала замедления
        self.reverse_stop_distance = 20  # Увеличено расстояние для полной остановки
        self.emergency_stop_distance = 15  # Увеличено расстояние для экстренной остановки
        self.min_reverse_speed = 0.2  # Уменьшена минимальная скорость
        self.reverse_steering_sensitivity = 1.5  # Немного уменьшена чувствительность
        self.reverse_left_steering_multiplier = 2.4

        # Параметры для перехода из REVERSE_RIGHT в REVERSE_LEFT
        # Для пустой дороги делаем переход быстрее (меньший угол и ближе к линии)
        if self.empty_road:
            self.reverse_right_to_left_angle = -41  # Быстрее: -35 вместо -47
            self.start_reverse_left_distance = -40  # Ближе к линии
        else:
            self.reverse_right_to_left_angle = -47  # Стандартный угол
            self.start_reverse_left_distance = -50  # Стандартное расстояние

        # Флаги для контроля столкновений
        self.collision_detected = False
        self.collision_cooldown = 0
        self.collision_count = 0

        # История углов для плавного управления
        self.angle_history = []

        # Флаг экстренной остановки
        self.emergency_stop = False
        self.emergency_stop_timer = 0

        # Боковые датчики для контроля безопасности
        self.side_safety_distance = 70  # Увеличено

        # Флаги для контроля выравнивания
        self.aligned_with_main_line = False
        self.alignment_tolerance = 15

        # Параметры для раннего начала REVERSE_LEFT
        self.reverse_left_steering_angle = -40
        self.reverse_left_speed = 0.5  # Уменьшена скорость

        # Флаги для избежания циклического поведения
        self.recently_backed_off = False
        self.backoff_timer = 0
        self.backoff_threshold = 1.5
        self.consecutive_backoffs = 0
        self.min_safe_distance = 20  # Минимальное безопасное расстояние

        # Дополнительные параметры для контроля задней части
        self.rear_collision_avoidance_enabled = True
        self.last_rear_distance = float('inf')
        self.rear_distance_decreasing = False

        # Новые параметры для циклического выравнивания в FINAL_ADJUST
        self.alignment_cycles = 0  # Счетчик выполненных циклов выравнивания
        self.max_alignment_cycles = 5  # Максимальное количество циклов
        self.alignment_state = "forward"  # Состояние цикла: "forward", "pause", "backward"
        self.alignment_state_timer = 0  # Таймер для каждого состояния
        self.alignment_forward_duration = 0.6  # Уменьшена длительность движения вперед
        self.alignment_backward_duration = 0.6  # Уменьшена длительность движения назад
        self.alignment_pause_duration = 0.3  # Длительность паузы между движениями
        self.initial_angle_when_aligned = 0  # Угол при входе в FINAL_ADJUST
        self.alignment_steering_angle = 10  # Уменьшен угол поворота при выравнивании
        self.alignment_speed = 0.15  # Медленная скорость для безопасности
        self.alignment_safety_distance = 40  # Минимальное безопасное расстояние

        # Новые флаги для управления циклами
        self.alignment_interrupted = False  # Флаг прерывания цикла
        self.last_alignment_state = None  # Последнее состояние цикла
        self.alignment_attempts = 0  # Попытки выравнивания после прерывания

    def _build_fuzzy_system(self):
        fs = FuzzySystem()

        # Входные переменные
        v_dist_right = FuzzyVariable("dist_right", 0, 200)
        v_dist_right.add_set("close", TrapezoidMF(0, 0, 50, 70))
        v_dist_right.add_set("optimal", TriangleMF(60, 90, 120))
        v_dist_right.add_set("far", TrapezoidMF(100, 150, 200, 200))
        fs.add_input(v_dist_right)

        v_dist_front = FuzzyVariable("dist_front", 0, 300)
        v_dist_front.add_set("danger", TrapezoidMF(0, 0, 60, 100))
        v_dist_front.add_set("safe", TrapezoidMF(80, 150, 300, 300))
        fs.add_input(v_dist_front)

        v_angle_err = FuzzyVariable("angle_error", -90, 90)
        v_angle_err.add_set("negative", TrapezoidMF(-90, -90, -10, -2))
        v_angle_err.add_set("zero", TriangleMF(-5, 0, 5))
        v_angle_err.add_set("positive", TrapezoidMF(2, 10, 90, 90))
        fs.add_input(v_angle_err)

        v_spot_dist = FuzzyVariable("spot_distance", 0, 500)
        v_spot_dist.add_set("far", TrapezoidMF(0, 100, 200, 300))
        v_spot_dist.add_set("near", TriangleMF(150, 250, 350))
        v_spot_dist.add_set("very_near", TrapezoidMF(300, 400, 500, 500))
        fs.add_input(v_spot_dist)

        # Выходные переменные
        v_steer = FuzzyVariable("steering", -40, 40)
        v_steer.add_set("left", TriangleMF(-40, -25, -5))
        v_steer.add_set("straight", TriangleMF(-5, 0, 5))
        v_steer.add_set("right", TriangleMF(5, 25, 40))
        fs.add_output(v_steer)

        v_throttle = FuzzyVariable("throttle", -2, 3)
        v_throttle.add_set("reverse_fast", TriangleMF(-2, -1.5, -1))
        v_throttle.add_set("reverse_slow", TriangleMF(-1.5, -1, -0.5))
        v_throttle.add_set("stop", TriangleMF(-0.5, 0, 0.5))
        v_throttle.add_set("slow", TriangleMF(0.5, 1.0, 1.5))
        v_throttle.add_set("fast", TriangleMF(1.5, 2.0, 2.5))
        fs.add_output(v_throttle)

        # Правила для разных фаз
        fs.add_rule(FuzzyRule([(v_dist_right, "close")], (v_steer, "left")))
        fs.add_rule(FuzzyRule([(v_dist_right, "optimal")], (v_steer, "straight")))
        fs.add_rule(FuzzyRule([(v_dist_right, "far")], (v_steer, "right")))
        fs.add_rule(FuzzyRule([(v_dist_front, "danger")], (v_throttle, "stop"), weight=3.0))
        fs.add_rule(FuzzyRule([(v_angle_err, "negative")], (v_steer, "right")))
        fs.add_rule(FuzzyRule([(v_angle_err, "positive")], (v_steer, "left")))

        return fs

    def update(self, car, spots, obstacles, dt):
        # Останавливаем время в фазе PARKED
        if self.phase != ParkingPhase.PARKED:
            self.phase_timer += dt

        # Обновляем кулдаун на столкновения
        if self.collision_cooldown > 0:
            self.collision_cooldown -= dt

        # Обновляем таймер отъезда
        if self.backoff_timer > 0:
            self.backoff_timer -= dt

        # Получаем данные с датчиков
        s_front = car.sensors[3]  # Передний датчик (-20°)
        s_right = car.sensors[5]  # Правый датчик (45°)
        s_left = car.sensors[2]  # Левый датчик (-45°)

        # Инициализируем front_distance значением по умолчанию
        front_distance = s_front

        # Используем исправленные задние датчики
        min_rear_distance = car.get_min_rear_distance()
        very_close_rear = car.get_very_close_rear()

        # Анализируем изменение заднего расстояния для предотвращения столкновений
        if hasattr(self, 'last_rear_distance'):
            self.rear_distance_decreasing = min_rear_distance < self.last_rear_distance - 5
        self.last_rear_distance = min_rear_distance

        # Проверяем точные столкновения
        has_collision_now, collided_with = car.check_collision_with_any(obstacles, margin=2)

        # Увеличиваем safety_margin для менее чувствительной проверки
        has_rear_collision_risk, rear_collision_distance = car.check_rear_collision(
            obstacles, self.safety_margin / 2  # Удваиваем запас
        )

        if has_collision_now:
            self.collision_count += 1
            print(f"СТОЛКНОВЕНИЕ #{self.collision_count}! Фаза: {self.phase.name}")
            car.speed = 0
            car.steering = 0

        inputs = {
            "dist_right": s_right,
            "dist_front": s_front,
            "angle_error": car.angle,
            "spot_distance": 500
        }

        # Если есть целевое место, вычисляем расстояние до него
        if self.target_spot:
            inputs["spot_distance"] = math.hypot(
                car.x - self.target_spot.x,
                car.y - self.target_spot.y
            )

        # Применяем нечеткую логику
        fuzzy_result = self.fuzzy_system.compute(inputs)

        # Базовая логика управления
        throttle = 0.0
        steering = 0.0
        reasoning = ""

        # Если обнаружено столкновение, останавливаемся
        if has_collision_now and self.phase not in [ParkingPhase.SEARCHING, ParkingPhase.APPROACH]:
            throttle = 0.0
            steering = 0.0
            reasoning = "СТОП: обнаружено столкновение!"
            car.speed = throttle
            car.steering = steering
            return

        # Обработка экстренной остановки
        if self.emergency_stop:
            self.emergency_stop_timer += dt
            throttle = 0.0
            steering = 0.0
            reasoning = "ЭКСТРЕННАЯ ОСТАНОВКА!"

            if self.emergency_stop_timer > 0.8:
                # Включаем режим восстановления
                self.emergency_stop = False
                self.emergency_stop_timer = 0
                self.recovering_from_emergency = True
                self.recovery_timer = 1.0  # 1 секунда на восстановление
                print("⚠️ Перехожу в режим восстановления после экстренной остановки")

            car.speed = throttle
            car.steering = steering
            return

        # Обработка восстановления после экстренной остановки
        if self.recovering_from_emergency:
            self.recovery_timer -= dt
            if self.recovery_timer > 0:
                # Во время восстановления отъезжаем от препятствия
                throttle = 0.25  # Медленно вперед

                if self.phase == ParkingPhase.REVERSE_RIGHT:
                    steering = -15  # Влево, чтобы изменить траекторию
                elif self.phase == ParkingPhase.REVERSE_LEFT:
                    steering = 15  # Вправо, чтобы изменить траекторию
                else:
                    steering = 0

                reasoning = f"Восстановление после экстренной остановки ({self.recovery_timer:.1f}с)"
            else:
                # Завершили восстановление
                self.recovering_from_emergency = False
                print("Восстановление завершено, продолжаю парковку")

            car.speed = throttle
            car.steering = steering
            return

        # Новая проверка выравнивания с линией - по всем углам машины
        line_alignment_distance, line_angle_deviation = car.get_line_alignment(self.target_line_y)
        angle_aligned = abs(car.angle) < 5  # Угол близок к 0° (параллельно линии)
        distance_aligned = line_alignment_distance < self.alignment_tolerance

        # Машина считается выровненной, если оба условия выполнены
        self.aligned_with_main_line = angle_aligned and distance_aligned

        if self.phase == ParkingPhase.SEARCHING:
            self.parking_complete = False
            self.stable_timer = 0
            self.collision_detected = False
            self.angle_history = []
            self.emergency_stop = False
            self.consecutive_backoffs = 0

            # Используем нечеткую логику для поиска
            throttle = fuzzy_result.get("throttle", 0.0)
            steering = fuzzy_result.get("steering", 0.0)
            reasoning = "Поиск места"

            # Принудительное движение вперед при поиске
            if throttle < 0.5 and s_front > 100:
                throttle = 2.0
                reasoning = "Ускорение: впереди нет препятствий"

            # Автоматический поиск места с помощью анализа сенсоров
            space_analysis = self.analyze_free_space(car, obstacles)

            if space_analysis['found'] and not self.target_spot:
                # Используем target_line_y для позиционирования места
                # Для пустой дороги это будет выше (дальше от бордюра)
                self.target_spot = ParkingSpot(
                    x=space_analysis['position_x'],
                    y=self.target_line_y,  # Используем целевую линию для Y-координаты
                    width=110,
                    length=140,
                    occupied=False,
                    gap_size=space_analysis['width']
                )
                self.phase = ParkingPhase.APPROACH
                self.phase_timer = 0
                print(f"✓ Найдено место анализом сенсоров!")
                print(f"  Ширина: {space_analysis['width']:.1f}, место сзади: {space_analysis['rear_space']:.1f}")
                print(
                    f"  Линия парковки: y={self.target_line_y} ({'пустая дорога' if self.empty_road else 'обычная дорога'})")
                print(f"⇨ Переход к фазе: ПОДЪЕЗД")

        elif self.phase == ParkingPhase.APPROACH:
            # Подъезжаем к месту
            if self.target_spot:
                target_x = self.target_spot.x + CAR_LENGTH * 1.8
                dist = target_x - car.x

                if dist > 50:
                    throttle = 1.5
                    # Корректируем положение относительно места
                    target_y = self.target_spot.y - CAR_WIDTH * 1.5
                    steering = -(car.y - target_y) * 0.01
                    reasoning = f"Подъезд к месту (осталось: {dist:.0f})"
                else:
                    throttle = 0.0
                    steering = 0.0
                    reasoning = "Остановка для маневра"
                    self.phase = ParkingPhase.POSITIONING
                    self.phase_timer = 0
                    print(f"⇨ Переход к фазе: ПОЗИЦИОНИРОВАНИЕ")

        elif self.phase == ParkingPhase.POSITIONING:
            # Комбинируем несколько условий для ранней остановки

            if self.target_spot:
                # 1. Проверяем расстояние до цели
                target_x = self.target_spot.x + CAR_LENGTH * 1.5
                dist_to_target = target_x - car.x

                # 2. Проверяем угол
                angle_ok = abs(car.angle) < 4

                # 3. Проверяем, движемся ли мы вперед
                moving_forward = car.speed > 0.1

                # Останавливаемся, если:
                # - Уже достаточно близко к цели (100 пикселей)
                # - ИЛИ угол уже хороший
                # - ИЛИ мы уже проехали мимо цели
                if (dist_to_target < 100 and angle_ok) or dist_to_target < -50:
                    throttle = 0.0
                    steering = 0.0
                    reasoning = f"Ранняя остановка (до цели: {dist_to_target:.0f}, угол: {car.angle:.1f}°)"

                    # Даем небольшую паузу для стабилизации
                    if self.phase_timer > 0.4:
                        self.phase = ParkingPhase.REVERSE_RIGHT
                        self.phase_timer = 0
                        self.collision_detected = False
                        self.consecutive_backoffs = 0
                        print(f"⇨ Переход к фазе: ЗАДНИЙ МАНЕВР (ВПРАВО)")
                else:
                    # Продолжаем позиционирование
                    throttle = 0.2  # Медленнее

                    # Комбинированное управление
                    angle_correction = -car.angle * 0.12
                    # Дополнительная коррекция положения
                    if dist_to_target > 0:
                        position_correction = min(0.1, dist_to_target * 0.002)
                    else:
                        position_correction = max(-0.1, dist_to_target * 0.002)

                    steering = angle_correction + position_correction

                    # Ограничиваем максимальный угол поворота
                    steering = max(-30, min(30, steering))

                    reasoning = f"Позиционирование (до цели: {dist_to_target:.0f}, угол: {car.angle:.1f}°)"
            else:
                # Резервная логика
                if abs(car.angle) > 3:
                    throttle = 0.2
                    steering = -car.angle * 0.1
                    reasoning = f"Выравнивание (угл: {car.angle:.1f}°)"
                else:
                    throttle = 0.0
                    steering = 0.0
                    reasoning = "Готов к парковке"
                    if self.phase_timer > 0.5:
                        self.phase = ParkingPhase.REVERSE_RIGHT
                        self.phase_timer = 0
                        self.collision_detected = False
                        self.consecutive_backoffs = 0
                        print(f"⇨ Переход к фазе: ЗАДНИЙ МАНЕВР (ВПРАВО)")

        elif self.phase == ParkingPhase.REVERSE_RIGHT:
            # Задний маневр с поворотом вправо
            max_speed_in_phase = 0.45  # Уменьшена

            # Сохраняем историю углов
            self.angle_history.append(car.angle)
            if len(self.angle_history) > 10:
                self.angle_history.pop(0)

            # УСИЛЕННЫЙ КОНТРОЛЬ БЕЗОПАСНОСТИ: проверяем несколько условий
            safety_conditions = [
                very_close_rear,
                min_rear_distance < self.emergency_stop_distance,
                has_rear_collision_risk,
                min_rear_distance < self.min_safe_distance and self.rear_distance_decreasing
            ]

            if any(safety_conditions):
                throttle = 0.0
                steering = 0.0
                self.emergency_stop = True
                self.emergency_stop_timer = 0
                reasoning = "ЭКСТРЕННАЯ ОСТАНОВКА: риск столкновения сзади!"
                print(f"⚠️ ЭКСТРЕННАЯ ОСТАНОВКА: заднее расстояние {min_rear_distance:.0f}")
            elif min_rear_distance < self.reverse_stop_distance:
                # Слишком близко - создаем зазор
                if self.consecutive_backoffs >= 2:  # Уменьшено с 3 до 2
                    # Слишком много попыток отъезда - переходим к следующей фазе
                    self.phase = ParkingPhase.REVERSE_LEFT
                    self.phase_timer = 0
                    self.collision_detected = False
                    self.consecutive_backoffs = 0
                    print(f"⇨ Вынужденный переход: слишком много отъездов")
                elif self.backoff_timer > 0:
                    # Пауза после отъезда
                    throttle = 0.0
                    steering = 0.0
                    reasoning = f"Пауза после отъезда: {self.backoff_timer:.1f}с"
                else:
                    # Отъезжаем вперед БЕЗ ПОВОРОТА для безопасности
                    throttle = 0.35
                    steering = 0.0  # Прямо, чтобы не усугубить ситуацию
                    reasoning = f"Создаю зазор: {min_rear_distance:.0f}"
                    self.backoff_timer = self.backoff_threshold
                    self.consecutive_backoffs += 1
                    print(f"⚠️ Создаю зазор #{self.consecutive_backoffs}, заднее: {min_rear_distance:.0f}")
            elif min_rear_distance < self.reverse_slow_distance:
                # Плавное замедление при приближении
                speed_factor = (min_rear_distance - self.reverse_stop_distance) / (
                        self.reverse_slow_distance - self.reverse_stop_distance
                )
                safe_speed = max(self.min_reverse_speed, max_speed_in_phase * speed_factor * 0.6)
                throttle = -safe_speed
                steering = 45 * self.reverse_steering_sensitivity  # Уменьшен угол
                reasoning = f"Замедление: {min_rear_distance:.0f}, скорость: {safe_speed:.2f}"
                # Сбрасываем счетчик отъездов при успешном движении
                if min_rear_distance > self.reverse_stop_distance + 25:
                    self.consecutive_backoffs = 0
            else:
                # Полная скорость, но с контролем
                throttle = -max_speed_in_phase * 0.7  # Уменьшена
                steering = 50 * self.reverse_steering_sensitivity  # Уменьшен угол
                reasoning = f"Задний маневр (угл: {car.angle:.1f}°, зад: {min_rear_distance:.0f})"
                # Сбрасываем счетчик отъездов при успешном движении
                self.consecutive_backoffs = 0

            # Переход в REVERSE_LEFT при достижении угла или расстояния до линии
            # Для пустой дороги переход происходит быстрее (при меньшем угле)
            if car.angle <= self.reverse_right_to_left_angle or line_alignment_distance < self.start_reverse_left_distance:
                self.phase = ParkingPhase.REVERSE_LEFT
                self.phase_timer = 0
                self.collision_detected = False
                self.consecutive_backoffs = 0
                print(f"⇨ Переход к фазе: ВЫРАВНИВАНИЕ (ВЛЕВО)")
                print(
                    f"Условие перехода: угол={car.angle:.1f}° (порог: {self.reverse_right_to_left_angle}°), до линии={line_alignment_distance:.0f} (порог: {self.start_reverse_left_distance})")
                print(f"Дорога пустая: {'ДА' if self.empty_road else 'НЕТ'}")

        elif self.phase == ParkingPhase.REVERSE_LEFT:
            # Активный поворот влево с повышенным контролем безопасности
            max_speed_in_phase = self.reverse_left_speed

            # УСИЛЕННЫЙ КОНТРОЛЬ БЕЗОПАСНОСТИ
            safety_conditions = [
                very_close_rear,
                min_rear_distance < self.emergency_stop_distance,
                has_rear_collision_risk,
                min_rear_distance < self.min_safe_distance and self.rear_distance_decreasing
            ]

            if any(safety_conditions):
                throttle = 0.0
                steering = 0.0
                self.emergency_stop = True
                self.emergency_stop_timer = 0
                reasoning = "ЭКСТРЕННАЯ ОСТАНОВКА: риск столкновения сзади!"
                print(f"⚠️ ЭКСТРЕННАЯ ОСТАНОВКА в REVERSE_LEFT: {min_rear_distance:.0f}")
            elif min_rear_distance < self.reverse_stop_distance:
                # Слишком близко - создаем зазор
                if self.consecutive_backoffs >= 2:
                    # Слишком много попыток - переходим к финальной корректировке
                    throttle = 0.0
                    steering = 0.0
                    self.phase = ParkingPhase.FINAL_ADJUST
                    self.phase_timer = 0
                    self.consecutive_backoffs = 0
                    print(f"⇨ Вынужденный переход к финальной корректировке")
                elif self.backoff_timer > 0:
                    # Пауза после отъезда
                    throttle = 0.0
                    steering = 0.0
                    reasoning = f"Пауза после отъезда: {self.backoff_timer:.1f}с"
                else:
                    # Отъезжаем вперед БЕЗ ПОВОРОТА для безопасности
                    throttle = 0.35
                    steering = 0.0  # Прямо
                    reasoning = f"Создаю зазор: {min_rear_distance:.0f}"
                    self.backoff_timer = self.backoff_threshold
                    self.consecutive_backoffs += 1
                    print(f"⚠️ Создаю зазор #{self.consecutive_backoffs} в REVERSE_LEFT")
            elif min_rear_distance < self.reverse_slow_distance:
                # Плавное замедление при приближении
                speed_factor = (min_rear_distance - self.reverse_stop_distance) / (
                        self.reverse_slow_distance - self.reverse_stop_distance
                )
                safe_speed = max(self.min_reverse_speed * 0.5, max_speed_in_phase * speed_factor * 0.5)
                throttle = -safe_speed

                # Активный поворот влево с регулировкой по углу
                if car.angle < -15:  # Если угол все еще отрицательный (машина наклонена вправо)
                    steering = self.reverse_left_steering_angle * self.reverse_left_steering_multiplier
                elif car.angle < -5:  # Близко к нулю
                    steering = -25  # Меньше
                elif car.angle > 10:  # Наклонены влево
                    steering = -self.reverse_left_steering_angle * 0.8  # Меньше
                else:  # Почти выровнены
                    steering = -20  # Еще меньше

                reasoning = f"Выравнивание ({min_rear_distance:.0f}, угол: {car.angle:.1f}°)"
                # Сбрасываем счетчик отъездов при успешном движении
                if min_rear_distance > self.reverse_stop_distance + 25:
                    self.consecutive_backoffs = 0
            else:
                # Движение назад с активным поворотом влево
                throttle = -max_speed_in_phase * 0.6  # Уменьшена

                # Регулируем силу поворота в зависимости от текущего угла
                if car.angle < -20:  # Сильно наклонены вправо
                    steering = self.reverse_left_steering_angle * 1.6  # Уменьшено
                elif car.angle < -8:  # Умеренно наклонены вправо
                    steering = self.reverse_left_steering_angle * 1.2  # Уменьшено
                elif car.angle > 12:  # Наклонены влево
                    steering = -self.reverse_left_steering_angle * 0.7  # Уменьшено
                else:  # Близко к выравнивания
                    steering = -15  # Еще меньше

                reasoning = f"Активное выравнивание (до линии: {line_alignment_distance:.0f}, угол: {car.angle:.1f}°)"
                # Сбрасываем счетчик отъездов при успешном движении
                self.consecutive_backoffs = 0

            # Проверка боковой безопасности
            if s_left < self.side_safety_distance:
                correction = (self.side_safety_distance - s_left) * 0.1  # Уменьшен коэффициент
                steering = min(steering + correction, 0)
                reasoning = f"Коррекция: близко слева ({s_left:.0f})"

            # Переход к финальной корректировке при выравнивании или по таймауту
            if (self.aligned_with_main_line) or self.phase_timer > 6.0:
                throttle = 0.0
                steering = 0.0
                self.phase = ParkingPhase.FINAL_ADJUST
                self.phase_timer = 0
                self.consecutive_backoffs = 0
                # Инициализация параметров циклического выравнивания
                self.alignment_cycles = 0
                self.alignment_state = "forward"
                self.alignment_state_timer = 0
                self.alignment_interrupted = False
                self.last_alignment_state = None
                self.alignment_attempts = 0
                self.initial_angle_when_aligned = car.angle
                print(f"⇨ Переход к фазе: ФИНАЛЬНАЯ КОРРЕКТИРОВКА")
                print(f"Начинаю циклическое выравнивание (максимум {self.max_alignment_cycles} циклов)")

        elif self.phase == ParkingPhase.FINAL_ADJUST:
            # Финальная корректировка с циклическими движениями вперед-назад
            # ПОНИЖЕННАЯ ЧУВСТВИТЕЛЬНОСТЬ ПЕРЕДНИХ ДАТЧИКОВ В ЭТОЙ ФАЗЕ

            # Используем оба передних датчиков для большей надежности
            front_left_sensor = car.sensors[3]  # -20° - главный передний
            front_right_sensor = car.sensors[4]  # 20° - правый передний
            # Используем максимальное расстояние (меньшая чувствительность)
            front_distance = max(front_left_sensor, front_right_sensor)

            if self.target_spot:
                # Проверка безопасности перед любым движением
                # УВЕЛИЧИЛИ порог срабатывания для меньшей чувствительности
                safety_conditions_forward = [
                    front_distance < 20 and car.speed > 0,  # Было < 40, теперь < 20 (меньшая чувствительность)
                    has_collision_now,
                ]

                safety_conditions_backward = [
                    very_close_rear,
                    min_rear_distance < self.emergency_stop_distance,
                    has_rear_collision_risk,
                ]

                # Обновляем таймер состояния
                self.alignment_state_timer += dt

                # Обработка прерываний из-за безопасности
                if any(safety_conditions_forward) and self.alignment_state == "forward":
                    throttle = 0.0
                    steering = 0.0
                    self.last_alignment_state = self.alignment_state
                    self.alignment_state = "pause"
                    self.alignment_state_timer = 0
                    self.alignment_interrupted = True
                    reasoning = f"Прервано: близко спереди ({front_distance:.0f})"
                    print(f"⚠️ Прервано движение вперед: спереди {front_distance:.0f}")

                elif any(safety_conditions_backward) and self.alignment_state == "backward":
                    throttle = 0.0
                    steering = 0.0
                    self.last_alignment_state = self.alignment_state
                    self.alignment_state = "pause"
                    self.alignment_state_timer = 0
                    self.alignment_interrupted = True
                    reasoning = f"Прервано: близко сзади ({min_rear_distance:.0f})"
                    print(f"⚠️ Прервано движение назад: сзади {min_rear_distance:.0f}")

                else:
                    # Выполняем нормальный цикл выравнивания
                    if self.alignment_cycles >= self.max_alignment_cycles:
                        # Завершаем парковку
                        throttle = 0.0
                        steering = 0.0
                        reasoning = f"Циклическое выравнивание завершено ({self.alignment_cycles}/{self.max_alignment_cycles} циклов)"

                        # Проверяем окончательную позицию
                        if car.x < self.target_spot.x - 10:
                            # Проверка безопасности перед движением вперед
                            if front_distance > 25:  # Увеличили порог с 40 до 25
                                throttle = 0.1
                                steering = 0.0
                                reasoning = "Финальный подъезд вперед"
                            else:
                                throttle = 0.0
                                steering = 0.0
                                reasoning = f"СТОП: близко спереди ({front_distance:.0f})"
                        elif car.x > self.target_spot.x + 10:
                            # Проверка безопасности перед движением назад
                            if min_rear_distance > 25:  # Увеличили порог с 40 до 25
                                throttle = -0.1
                                steering = 0.0
                                reasoning = "Финальный подъезд назад"
                            else:
                                throttle = 0.0
                                steering = 0.0
                                reasoning = f"СТОП: близко сзади ({min_rear_distance:.0f})"
                        else:
                            # Переходим в фазу PARKED
                            self.phase = ParkingPhase.PARKED
                            self.phase_timer = 0
                            print(f"⇨ Переход к фазе: ПАРКОВКА ЗАВЕРШЕНА")
                    else:
                        # Выполняем циклическое выравнивание
                        if self.alignment_state == "forward":
                            # Движение вперед с поворотом для выравнивания
                            throttle = self.alignment_speed * 0.8

                            # Определяем направление поворота для выравнивания
                            if car.angle > 1:  # Машина наклонена влево
                                steering = -self.alignment_steering_angle * 5  # Поворачиваем вправо
                                steering_direction = "вправо"
                            elif car.angle < -1:  # Машина наклонена вправо
                                steering = self.alignment_steering_angle * 5  # Поворачиваем влево
                                steering_direction = "влево"
                            else:  # Почти ровно
                                steering = 0
                                steering_direction = "прямо"

                            reasoning = f"Цикл {self.alignment_cycles + 1}/{self.max_alignment_cycles}: Вперед, руль {steering_direction} ({car.angle:.1f}°)"

                            # Проверяем завершение движения вперед
                            if self.alignment_state_timer >= self.alignment_forward_duration:
                                self.last_alignment_state = self.alignment_state
                                self.alignment_state = "pause"
                                self.alignment_state_timer = 0
                                reasoning = f"Пауза между движениями"

                        elif self.alignment_state == "pause":
                            # Пауза между движениями
                            throttle = 0.0
                            steering = 0.0

                            # Определяем, что делать после паузы
                            if self.alignment_state_timer >= self.alignment_pause_duration:
                                if self.alignment_interrupted:
                                    # После прерывания продолжаем тот же цикл
                                    self.alignment_state = self.last_alignment_state
                                    self.alignment_attempts += 1
                                    reasoning = f"Повтор цикла после прерывания (попытка {self.alignment_attempts})"

                                    # Если слишком много попыток, переходим к следующему циклу
                                    if self.alignment_attempts >= 2:
                                        self.alignment_cycles += 1
                                        self.alignment_attempts = 0
                                        reasoning = f"Пропуск цикла {self.alignment_cycles}/{self.max_alignment_cycles} после неудачи"
                                else:
                                    # Нормальное чередование
                                    if self.last_alignment_state == "forward":
                                        self.alignment_state = "backward"
                                    elif self.last_alignment_state == "backward":
                                        self.alignment_state = "forward"

                                self.alignment_state_timer = 0
                                self.alignment_interrupted = False
                            else:
                                reasoning = f"Пауза ({self.alignment_state_timer:.1f}/{self.alignment_pause_duration}с)"

                        elif self.alignment_state == "backward":
                            # Движение назад с противоположным поворотом
                            throttle = -self.alignment_speed * 0.8

                            # Противоположный поворот для компенсации
                            if car.angle > 1:  # Машина наклонена влево
                                steering = self.alignment_steering_angle * 5  # Поворачиваем влево
                                steering_direction = "влево"
                            elif car.angle < -1:  # Машина наклонена вправо
                                steering = -self.alignment_steering_angle * 5  # Поворачиваем вправо
                                steering_direction = "вправо"
                            else:  # Почти ровно
                                steering = 0
                                steering_direction = "прямо"

                            reasoning = f"Цикл {self.alignment_cycles + 1}/{self.max_alignment_cycles}: Назад, руль {steering_direction} ({car.angle:.1f}°)"

                            # Проверяем завершение движения назад
                            if self.alignment_state_timer >= self.alignment_backward_duration:
                                self.last_alignment_state = self.alignment_state
                                self.alignment_state = "pause"
                                self.alignment_state_timer = 0
                                self.alignment_cycles += 1
                                self.alignment_attempts = 0
                                print(
                                    f"Завершен цикл выравнивания {self.alignment_cycles}/{self.max_alignment_cycles}, угол: {car.angle:.1f}°")

                                # Если достигли хорошего угла, можно завершить раньше
                                if abs(car.angle) < 1.0 and line_alignment_distance < 10:
                                    self.alignment_cycles = self.max_alignment_cycles  # Завершаем
                                    reasoning = f"Отличное выравнивание! Угол: {car.angle:.1f}°, до линии: {line_alignment_distance:.0f}"

        elif self.phase == ParkingPhase.PARKED:
            throttle = 0.0
            steering = 0.0
            reasoning = "Парковка завершена"

            if abs(car.speed) < 0.1 and abs(car.angle) < 1.0:
                self.stable_timer += dt
                if self.stable_timer > 1.0:
                    self.parking_complete = True
            else:
                self.stable_timer = 0

        # Применяем управление
        car.speed = throttle
        car.steering = steering

        # Отладочная информация
        self.debug_info = {
            "Phase": self.phase.name,
            "Reasoning": reasoning,
            "Throttle": f"{throttle:.2f}",
            "Steering": f"{steering:.2f}",
            "Rear_Dist": f"{min_rear_distance:.0f}",
            "Left_Side": f"{s_left:.0f}",
            "To_Line_Dist": f"{line_alignment_distance:.0f}",
            "Line_Angle": f"{line_angle_deviation:.1f}°",
            "Aligned": "ДА" if self.aligned_with_main_line else "НЕТ",
            "Very_Close": "ДА" if very_close_rear else "НЕТ",
            "Emergency": "ДА" if self.emergency_stop else "НЕТ",
            "Collision": "ДА" if has_collision_now else "НЕТ",
            "Col_Count": f"{self.collision_count}",
            "Backoffs": f"{self.consecutive_backoffs}",
            "Backoff_Timer": f"{self.backoff_timer:.1f}",
            "Rear_Risk": "ДА" if has_rear_collision_risk else "НЕТ",
            "Rear_Decr": "ДА" if self.rear_distance_decreasing else "НЕТ",
            "Car_Angle": f"{car.angle:.1f}°",
            "Car_X": f"{car.x:.0f}",
            "Car_Y": f"{car.y:.0f}",
            "Align_Cycles": f"{self.alignment_cycles}/{self.max_alignment_cycles}" if self.phase == ParkingPhase.FINAL_ADJUST else "N/A",
            "Align_State": self.alignment_state if self.phase == ParkingPhase.FINAL_ADJUST else "N/A",
            "Front_Dist": f"{front_distance:.0f}",
            "Align_Attempts": f"{self.alignment_attempts}" if self.phase == ParkingPhase.FINAL_ADJUST else "N/A",
            "Empty_Road": "ДА" if self.empty_road else "НЕТ",
            "Target_Line_Y": f"{self.target_line_y}",
            "R2L_Angle": f"{self.reverse_right_to_left_angle}°",
            "R2L_Dist": f"{self.start_reverse_left_distance}"
        }

    def analyze_free_space(self, car, obstacles):
        """Анализирует свободное пространство с помощью сенсоров"""
        # Получаем расстояния с нескольких датчиков
        right_distances = [
            min(car.sensors[5], 200),  # Правый 45°
            min(car.sensors[6], 200),  # Правый 90°
            min(car.sensors[7], 200)  # Правый 135°
        ]

        # Проверяем, достаточно ли места по всем датчикам
        avg_right_distance = sum(right_distances) / len(right_distances)

        # Получаем заднее расстояние
        rear_distance = car.get_min_rear_distance()

        # ОСОБЫЙ СЛУЧАЙ: при пустой дороге (все датчики показывают максимальные значения)
        # Используем необработанные значения датчиков для определения, пустая ли дорога
        raw_right_distances = [car.sensors[5], car.sensors[6], car.sensors[7]]
        avg_raw_right_distance = sum(raw_right_distances) / len(raw_right_distances)

        # Если дорога пустая (все датчики показывают почти максимальное значение)
        # и есть достаточно места сзади, создаем виртуальное место в правильной позиции
        if avg_raw_right_distance > 380 and rear_distance > 300 and len(
                obstacles) <= 2:  # <= 2 потому что есть виртуальные границы
            # Дорога пустая - создаем виртуальное место на правильном расстоянии от бордюра
            # Позиция должна быть такой, чтобы при парковке машина была правильно расположена относительно бордюра
            # Используем target_line_y, который уже установлен в зависимости от того, пустая ли дорога
            return {
                'found': True,
                'width': CAR_LENGTH * 3,  # Широкое место
                'position_x': car.x + 400,  # Смещаем место вперед от текущей позиции
                'position_y': self.target_line_y,  # Используем целевую линию (для пустой дороги она выше)
                'rear_space': rear_distance
            }

        # Обычная логика для непустых дорог
        if avg_right_distance > CAR_LENGTH * 2.0:
            if rear_distance > CAR_LENGTH:
                return {
                    'found': True,
                    'width': avg_right_distance,
                    'position_x': car.x,
                    'position_y': self.target_line_y,  # Используем целевую линию
                    'rear_space': rear_distance
                }

        return {'found': False}


class HybridParkingSimulation:
    def __init__(self):
        pygame.init()
        self.screen = pygame.display.set_mode((WIDTH, HEIGHT))
        pygame.display.set_caption("ПАРАЛЛЕЛЬНАЯ ПАРКОВКА")
        self.clock = pygame.time.Clock()
        # Единый шрифт для всего текста
        self.ui_font = pygame.font.SysFont("Arial", 20)
        self.small_font = pygame.font.SysFont("Arial", 16)

        # Инициализация
        self.reset()
        self.start_time = pygame.time.get_ticks()

        # Флаги отображения
        self.show_sensors = True  # Новый флаг для датчиков
        self.show_alignment_lines = True  # Флаг для линий выравнивания
        self.show_help = False  # Флаг для показа справки
        self.show_labels = True  # Флаг для надписей

        # НОВЫЙ ФЛАГ: пауза игры
        self.paused = False  # По умолчанию игра не на паузе

        # Флаг для определения пустой дороги
        self.empty_road = False

    def reset(self):
        # Инициализация пустых списков
        self.obstacle_cars = []
        self.parking_spots = []

        # Генерация случайной улицы
        self.generate_random_street()

        # Игрок
        self.player_car = Car(50, HEIGHT - 220, 0)

        # Контроллер с передачей информации о пустой дороге
        self.controller = HybridParkingController(empty_road=self.empty_road)

        # UI состояние
        self.parked = False
        self.paused = False  # Сбрасываем паузу при рестарте

        self.target_spot = None

    def generate_random_street(self):
        """Генерация случайной улицы с машинами и парковочными местами между ними"""
        self.obstacle_cars = []
        self.parking_spots = []  # Очищаем список парковочных мест

        num_obstacles = random.randint(0, 6)

        # Устанавливаем флаг пустой дороги
        self.empty_road = (num_obstacles == 0)

        # ОСОБЫЙ СЛУЧАЙ: при пустой дороге создаем виртуальные препятствия по краям
        # чтобы задать правильные границы для парковки
        if self.empty_road:
            # Создаем два виртуальных препятствия по краям, чтобы задать границы парковки
            # Они будут невидимыми, но обеспечат правильное поведение датчиков
            left_boundary = Car(-100, HEIGHT - 150, 0, (150, 50, 50))
            right_boundary = Car(WIDTH + 100, HEIGHT - 150, 0, (150, 50, 50))
            self.obstacle_cars.append(left_boundary)
            self.obstacle_cars.append(right_boundary)

            # Также создаем виртуальное парковочное место в середине дороги
            # чтобы контроллер мог его обнаружить
            # Используем линию парковки, которая будет соответствовать target_line_y в контроллере
            # Для пустой дороги это будет HEIGHT - 180 (приподнятая линия)
            self.parking_spots.append(ParkingSpot(
                x=WIDTH // 2,
                y=HEIGHT - 180,  # Приподнятая линия для пустой дороги
                width=110,
                length=140,
                occupied=False,
                gap_size=300  # Широкий промежуток для пустой дороги
            ))
            print(f"Создана пустая дорога с виртуальными границами и местом для парковки")
            print(f"Линия парковки приподнята до y={HEIGHT - 180} (обычная: y={HEIGHT - 150})")
            print(f"Переход из REVERSE_RIGHT в REVERSE_LEFT будет быстрее (угол: -35° вместо -47°)")
        else:
            # Обычная генерация препятствий
            current_x = 100
            for i in range(num_obstacles):
                # Добавляем машину-препятствие
                car = Car(current_x, HEIGHT - 150, 0, (150, 50, 50))
                self.obstacle_cars.append(car)

                # Сдвигаем позицию для следующей машины
                current_x += CAR_LENGTH + random.uniform(20, CAR_LENGTH * 2.5)
                if current_x > WIDTH:
                    break

        # Вывод информации о созданных местах
        print(f"\nСгенерировано {len(self.obstacle_cars)} машин-препятствий")
        print(f"Найдено {len(self.parking_spots)} парковочных мест")
        print(f"Дорога пустая: {'ДА' if self.empty_road else 'НЕТ'}")

    def run(self):
        running = True
        while running:
            dt = self.clock.tick(FPS) / 1000.0

            # Обработка событий
            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    running = False
                elif event.type == pygame.KEYDOWN:
                    if event.key == pygame.K_r:
                        print("\n" + "=" * 60)
                        print("ПЕРЕЗАПУСК СИСТЕМЫ...")
                        print("=" * 60)
                        self.reset()
                    elif event.key == pygame.K_s:
                        self.show_sensors = not self.show_sensors
                        print(f"Датчики: {'ВКЛ' if self.show_sensors else 'ВЫКЛ'}")
                    elif event.key == pygame.K_a:
                        self.show_alignment_lines = not self.show_alignment_lines
                        print(f"Линии выравнивания: {'ВКЛ' if self.show_alignment_lines else 'ВЫКЛ'}")
                    elif event.key == pygame.K_h:
                        # ИЗМЕНЕНО: теперь H включает/выключает паузу и показывает/скрывает справку
                        self.paused = not self.paused
                        # Показываем справку только когда игра на паузе
                        self.show_help = self.paused
                        if self.paused:
                            print("ИГРА НА ПАУЗЕ (нажмите H для продолжения)")
                        else:
                            print("ИГРА ПРОДОЛЖАЕТСЯ")
                    elif event.key == pygame.K_n:
                        self.show_labels = not self.show_labels
                        print(f"Надписи: {'ВКЛ' if self.show_labels else 'ВЫКЛ'}")

            # Если игра на паузе, не обновляем состояние
            if self.paused:
                # Обновляем только отрисовку
                self.draw()
                pygame.display.flip()
                continue

            # Обновление сенсоров (только если игра не на паузе)
            all_obstacles = self.obstacle_cars.copy()
            self.player_car.update_sensors(all_obstacles)

            # Обновление позиции машины
            self.player_car.update(dt)

            # Обновление контроллера
            self.controller.update(self.player_car, self.parking_spots, all_obstacles, dt)

            # Отрисовка
            self.draw()

            pygame.display.flip()

        pygame.quit()
        sys.exit()

    def draw(self):
        # Фон
        self.screen.fill((40, 44, 52))

        # Дорога
        pygame.draw.rect(self.screen, (80, 80, 90), (0, HEIGHT - 300, WIDTH, 300))

        # Разметка дороги
        for i in range(0, WIDTH, 60):
            pygame.draw.line(self.screen, (255, 255, 200),
                             (i, HEIGHT - 200), (i + 30, HEIGHT - 200), 3)

        # Бордюр (основная линия автомобилей)
        curb_y = HEIGHT - 115
        pygame.draw.line(self.screen, (255, 200, 0),
                         (0, curb_y), (WIDTH, curb_y), 5)

        # Область ниже бордюра (тротуар) - желтым цветом
        pygame.draw.rect(self.screen, (255, 255, 150, 100),
                         (0, curb_y + 1, WIDTH, HEIGHT - (curb_y + 1)))

        # Отметка основной линии (пунктирная)
        if self.show_alignment_lines:
            for i in range(0, WIDTH, 40):
                pygame.draw.line(self.screen, (255, 255, 100, 150),
                                 (i, curb_y), (i + 20, curb_y), 2)

        # Машины-препятствия (без датчиков, так как они стоят)
        # Не рисуем виртуальные границы (те, что за пределами экрана)
        for car in self.obstacle_cars:
            # Пропускаем виртуальные границы за пределами экрана
            if car.x < -50 or car.x > WIDTH + 50:
                continue
            car.draw(self.screen, draw_sensors=False)

        # Игрок (с датчиками если включено)
        self.player_car.draw(self.screen,
                             draw_sensors=self.show_sensors,
                             show_labels=self.show_labels)

        # Визуализация основной линии выравнивания
        if self.show_alignment_lines and self.controller.phase in [ParkingPhase.REVERSE_RIGHT,
                                                                   ParkingPhase.REVERSE_LEFT,
                                                                   ParkingPhase.FINAL_ADJUST] and self.show_labels:
            line_y = self.controller.target_line_y

            # Рисуем саму линию выравнивания
            line_color = (100, 255, 100, 200)
            pygame.draw.line(self.screen, line_color, (0, line_y), (WIDTH, line_y), 2)

            # Подписываем линию
            line_text = self.ui_font.render(f"Линия выравнивания (y={line_y})", True, line_color)
            self.screen.blit(line_text, (WIDTH // 2 - 100, line_y - 25))

            # Визуализация расстояния от углов машины до линии
            corners = self.player_car.get_corners()
            for corner in corners:
                pygame.draw.line(self.screen, (255, 100, 100, 150),
                                 (corner[0], corner[1]), (corner[0], line_y), 1)

            # Показываем расстояние до линии (минимальное от углов)
            line_distance, line_angle = self.player_car.get_line_alignment(line_y)
            dist_color = (100, 255, 100) if line_distance < self.controller.alignment_tolerance else (255, 255, 100)
            dist_text = self.ui_font.render(f"До линии (углы): {line_distance:.0f}", True, dist_color)
            self.screen.blit(dist_text, (self.player_car.x + 30, self.player_car.y - 40))

            # Показываем угол отклонения
            angle_text = self.ui_font.render(f"Угол к линии: {line_angle:.1f}°", True, dist_color)
            self.screen.blit(angle_text, (self.player_car.x + 30, self.player_car.y - 60))

        # Визуализация выбранного места
        if self.controller.target_spot and self.show_labels:
            spot = self.controller.target_spot

            pygame.draw.line(self.screen, (255, 255, 0, 150),
                             (self.player_car.x, self.player_car.y),
                             (spot.x, spot.y), 2)

        # UI
        self.draw_ui()

        # Окно справки (показывается только при паузе)
        if self.show_help:
            self.draw_help_window()

        # Сообщение об успешной парковке
        if self.controller.phase == ParkingPhase.PARKED and self.controller.parking_complete:
            self.draw_success_message()

        # НОВОЕ: Отображение сообщения о паузе
        if self.paused:
            self.draw_pause_message()

    def draw_ui(self):
        # Если надписи выключены, рисуем только нижние две надписи
        if not self.show_labels:
            # Управление внизу экрана (только две надписи)
            controls_y = HEIGHT - 50
            controls = [
                "R - ПЕРЕЗАПУСК",
                "H - ПАУЗА/СПРАВКА"
            ]

            control_width = WIDTH // len(controls)
            for i, control in enumerate(controls):
                control_text = self.ui_font.render(control, True, (41, 49, 51))
                x_pos = i * control_width + control_width // 2 - control_text.get_width() // 2
                self.screen.blit(control_text, (x_pos, controls_y))
            return

        y_offset = 20

        # Заголовок
        title = self.ui_font.render("ПАРАЛЛЕЛЬНАЯ ПАРКОВКА", True, (255, 255, 200))
        self.screen.blit(title, (WIDTH // 2 - title.get_width() // 2, y_offset))
        y_offset += 40

        # Информация об управлении
        control_box = pygame.Rect(50, y_offset, WIDTH - 100, 100)
        pygame.draw.rect(self.screen, (60, 70, 60), control_box, border_radius=5)
        pygame.draw.rect(self.screen, (100, 150, 100), control_box, 2, border_radius=5)

        speed_color = (200, 255, 200) if self.player_car.speed > 0 else (
            (255, 200, 200) if self.player_car.speed < 0 else (200, 200, 200))

        speed_text = self.ui_font.render(f"Скорость: {self.player_car.speed:.2f}",
                                         True, speed_color)
        self.screen.blit(speed_text, (70, y_offset + 20))

        steer_text = self.ui_font.render(f"Руль: {self.player_car.steering:.1f}°",
                                         True, (220, 220, 200))
        self.screen.blit(steer_text, (70, y_offset + 50))

        reasoning = self.controller.debug_info.get('Reasoning', '')
        if len(reasoning) > 35:
            reasoning = reasoning[:35] + "..."

        reason_color = (255, 240, 200)
        reason_text = self.ui_font.render(f"Действие: {reasoning}", True, reason_color)
        self.screen.blit(reason_text, (WIDTH // 2 + 50, y_offset + 20))

        # Фаза
        phase_color = (255, 200, 100)
        phase_label_text = self.ui_font.render(f"Фаза: {self.controller.phase.name}", True, phase_color)
        self.screen.blit(phase_label_text, (WIDTH // 2 + 50, y_offset + 50))

        # Управление внизу экрана
        controls_y = HEIGHT - 50
        controls = [
            "R - ПЕРЕЗАПУСК",
            "H - ПАУЗА/СПРАВКА"
        ]

        control_width = WIDTH // len(controls)
        for i, control in enumerate(controls):
            control_text = self.ui_font.render(control, True, (41, 49, 51))
            x_pos = i * control_width + control_width // 2 - control_text.get_width() // 2
            self.screen.blit(control_text, (x_pos, controls_y))

    def draw_help_window(self):
        # Полупрозрачный фон
        overlay = pygame.Surface((WIDTH, HEIGHT), pygame.SRCALPHA)
        overlay.fill((0, 0, 0, 180))
        self.screen.blit(overlay, (0, 0))

        # Окно справки
        help_rect = pygame.Rect(WIDTH // 2 - 350, HEIGHT // 2 - 200, 730, 420)
        pygame.draw.rect(self.screen, (30, 40, 50), help_rect, border_radius=10)
        pygame.draw.rect(self.screen, (100, 150, 200), help_rect, 3, border_radius=10)

        # Заголовок - ИЗМЕНЕНО: добавлено "ПАУЗА"
        title = self.ui_font.render("СПРАВКА ПО УПРАВЛЕНИЮ", True, (255, 255, 200))
        self.screen.blit(title, (help_rect.centerx - title.get_width() // 2, help_rect.top + 20))

        # Список команд - ИЗМЕНЕНО: теперь H для паузы
        commands = [
            ("R", "Перезапуск"),
            ("H", "Справка"),
            ("S", "Лучи датчиков"),
            ("A", "Линии выравнивания"),
            ("N", "Скрыть/показать надписи"),
            ("", ""),
            ("ФАЗЫ ПАРКОВКИ:", ""),
            ("SEARCHING", "Поиск парковочного места"),
            ("APPROACH", "Подъезд к месту"),
            ("POSITIONING", "Позиционирование для маневра"),
            ("REVERSE_RIGHT", "Задний ход с поворотом вправо"),
            ("REVERSE_LEFT", "Задний ход с поворотом влево"),
            ("FINAL_ADJUST", "5 циклов выравнивания вперед-назад"),
            ("PARKED", "Парковка завершена (время остановлено)"),
            ("", ""),
        ]

        y_offset = help_rect.top + 70
        for key, desc in commands:
            if key == "" and desc == "":
                y_offset += 10
                continue

            if "ФАЗЫ" in key or "ОСОБЕННОСТИ" in key:
                # Подзаголовок
                text = self.ui_font.render(key, True, (255, 200, 100))
                self.screen.blit(text, (help_rect.left + 30, y_offset))
                y_offset += 25
            else:
                key_text = self.ui_font.render(f"{key}:", True, (200, 220, 255))
                self.screen.blit(key_text, (help_rect.left + 30, y_offset))

                desc_text = self.ui_font.render(desc, True, (220, 240, 255))
                self.screen.blit(desc_text, (help_rect.left + 200, y_offset))
                y_offset += 25

    def draw_success_message(self):
        overlay = pygame.Surface((WIDTH, HEIGHT), pygame.SRCALPHA)
        overlay.fill((0, 0, 0, 150))
        self.screen.blit(overlay, (0, 0))

        success_box = pygame.Rect(WIDTH // 2 - 250, HEIGHT // 2 - 100, 500, 200)
        pygame.draw.rect(self.screen, (30, 60, 30), success_box, border_radius=10)
        pygame.draw.rect(self.screen, (100, 200, 100), success_box, 4, border_radius=10)

        success_text = self.ui_font.render("ПАРКОВКА ЗАВЕРШЕНА!", True, (100, 255, 100))
        self.screen.blit(success_text,
                         (WIDTH // 2 - success_text.get_width() // 2, HEIGHT // 2 - 70))

        restart_text = self.ui_font.render("Нажмите R для новой парковки",
                                           True, (150, 255, 150))
        self.screen.blit(restart_text,
                         (WIDTH // 2 - restart_text.get_width() // 2, HEIGHT // 2 + 50))

    def draw_pause_message(self):
        # Полупрозрачный фон
        overlay = pygame.Surface((WIDTH, HEIGHT), pygame.SRCALPHA)
        overlay.fill((0, 0, 0, 120))
        self.screen.blit(overlay, (0, 0))


def main():
    simulation = HybridParkingSimulation()
    simulation.run()


if __name__ == "__main__":
    main()