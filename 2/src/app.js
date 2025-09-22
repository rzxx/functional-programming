let state = {
  tasks: [],
  currentFilter: "all", // 'all', 'active', 'completed'
};

const taskForm = document.querySelector(".task-form");
const taskInput = document.querySelector(".task-input");
const taskList = document.querySelector(".task-list");
const filterButtons = document.querySelector(".filter-buttons");

const addTask = (tasks, text) => [
  ...tasks,
  {
    id: Date.now(),
    text: text,
    completed: false,
  },
];

const toggleTask = (tasks, id) =>
  tasks.map((task) =>
    task.id === id ? { ...task, completed: !task.completed } : task
  );

const deleteTask = (tasks, id) => tasks.filter((task) => task.id !== id);

const filterTasks = (tasks, filter) => {
  switch (filter) {
    case "active":
      return tasks.filter((task) => !task.completed);
    case "completed":
      return tasks.filter((task) => task.completed);
    default:
      return tasks;
  }
};

const render = () => {
  const filtered = filterTasks(state.tasks, state.currentFilter);

  taskList.innerHTML = "";

  filtered.forEach((task) => {
    const taskItem = document.createElement("li");
    taskItem.classList.add("task-item");
    if (task.completed) {
      taskItem.classList.add("completed");
    }
    taskItem.dataset.id = task.id;

    taskItem.innerHTML = `
            <input type="checkbox" class="task-item-checkbox" ${
              task.completed ? "checked" : ""
            }>
            <span class="task-item-text">${task.text}</span>
            <button class="task-item-delete-btn">X</button>
        `;

    taskList.appendChild(taskItem);
  });

  const btns = filterButtons.querySelectorAll(".filter-btn");
  btns.forEach((btn) =>
    btn.classList.toggle("active", btn.dataset.filter === state.currentFilter)
  );
};

taskForm.addEventListener("submit", (event) => {
  event.preventDefault();
  const newTaskText = taskInput.value.trim();

  if (newTaskText) {
    state = {
      ...state,
      tasks: addTask(state.tasks, newTaskText),
    };
    taskInput.value = "";
    render();
  }
});

taskList.addEventListener("click", (event) => {
  const target = event.target;
  const taskItem = target.closest(".task-item");
  if (!taskItem) return;

  const taskId = Number(taskItem.dataset.id);

  if (target.classList.contains("task-item-checkbox")) {
    state = {
      ...state,
      tasks: toggleTask(state.tasks, taskId),
    };
  }

  if (target.classList.contains("task-item-delete-btn")) {
    state = {
      ...state,
      tasks: deleteTask(state.tasks, taskId),
    };
  }

  render();
});

filterButtons.addEventListener("click", (event) => {
  if (event.target.classList.contains("filter-btn")) {
    const newFilter = event.target.dataset.filter;
    state = {
      ...state,
      currentFilter: newFilter,
    };
    render();
  }
});

render();
