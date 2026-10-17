using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BankingSystem
{
	// ===================== МОДЕЛЬ =====================

	public abstract class BankAccount
	{
		private static int nextNumber = 1001;   // общий счётчик номеров для всех счетов

		protected decimal balance;
		private readonly List<string> history = new List<string>();

		public int Number { get; }
		public string Owner { get; }
		public decimal Balance => balance;
		public IReadOnlyList<string> History => history;

		// Каждый наследник сообщает, как называется его тип счёта
		public abstract string TypeName { get; }

		protected BankAccount(string owner, decimal initialBalance)
		{
			if (string.IsNullOrWhiteSpace(owner))
			{
				throw new ArgumentException("Имя владельца не может быть пустым.");
			}
			if (initialBalance < 0)
			{
				throw new ArgumentException("Начальный баланс не может быть отрицательным.");
			}

			Owner = owner.Trim();
			balance = initialBalance;
			Number = nextNumber++;
			AddHistory($"Открыт счёт. Баланс: {Money(balance)}");
		}

		public static string Money(decimal value) => $"{value:N2} сом";

		protected void AddHistory(string text)
		{
			history.Add($"{DateTime.Now:dd.MM.yyyy HH:mm:ss}  {text}");
		}

		public virtual bool Deposit(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма пополнения должна быть больше 0.");
				return false;
			}
			balance += amount;
			AddHistory($"Пополнение +{Money(amount)}. Баланс: {Money(balance)}");
			Console.WriteLine($"[Пополнение] Зачислено: {Money(amount)}. Текущий баланс: {Money(balance)}");
			return true;
		}

		// true — снятие прошло, false — отклонено
		public abstract bool Withdraw(decimal amount);

		public override string ToString()
		{
			return $"№{Number} | {TypeName,-14} | {Owner,-15} | {Money(balance),15}";
		}
	}

	public class SavingsAccount : BankAccount
	{
		private readonly decimal minBalance;

		public override string TypeName => "Сберегательный";

		public SavingsAccount(string owner, decimal initialBalance, decimal minBalance)
			: base(owner, initialBalance)
		{
			if (initialBalance < minBalance)
			{
				throw new ArgumentException($"Начальный баланс сберегательного счёта не может быть меньше {Money(minBalance)}.");
			}
			this.minBalance = minBalance;
		}

		public override bool Withdraw(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма снятия должна быть больше 0.");
				return false;
			}

			if (balance - amount < minBalance)
			{
				Console.WriteLine($"[Отклонено] На сберегательном счёте должен оставаться минимальный остаток не менее {Money(minBalance)}. Текущий баланс: {Money(balance)}");
				return false;
			}

			balance -= amount;
			AddHistory($"Снятие -{Money(amount)}. Баланс: {Money(balance)}");
			Console.WriteLine($"[Снятие] Со сберегательного счёта снято: {Money(amount)}. Остаток: {Money(balance)}");
			return true;
		}
	}

	public class CheckingAccount : BankAccount
	{
		private readonly decimal serviceFee;

		public override string TypeName => "Расчётный";

		public CheckingAccount(string owner, decimal initialBalance, decimal serviceFee)
			: base(owner, initialBalance)
		{
			this.serviceFee = serviceFee;
		}

		public override bool Withdraw(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма снятия должна быть больше 0.");
				return false;
			}

			decimal totalDeduction = amount + serviceFee;

			if (balance < totalDeduction)
			{
				Console.WriteLine($"[Отклонено] Недостаточно средств для снятия {Money(amount)} с учётом комиссии {Money(serviceFee)}. Текущий баланс: {Money(balance)}");
				return false;
			}

			balance -= totalDeduction;
			AddHistory($"Снятие -{Money(amount)} (комиссия {Money(serviceFee)}). Баланс: {Money(balance)}");
			Console.WriteLine($"[Снятие] С расчётного счёта снято: {Money(amount)} (комиссия: {Money(serviceFee)}). Остаток: {Money(balance)}");
			return true;
		}
	}

	// ===================== ПРОГРАММА =====================

	class Program
	{
		// Параметры банка (можно менять)
		const decimal SavingsMinBalance = 1000m;
		const decimal CheckingFee = 50m;

		static readonly List<BankAccount> accounts = new List<BankAccount>();

		static void Main(string[] args)
		{
			Console.OutputEncoding = Encoding.UTF8;
			Console.InputEncoding = Encoding.UTF8;

			Console.WriteLine("=== СИСТЕМА УПРАВЛЕНИЯ БАНКОВСКИМИ СЧЕТАМИ ===");

			bool running = true;
			while (running)
			{
				ShowMenu();
				string choice = ReadLine("Ваш выбор: ");
				Console.WriteLine();

				switch (choice)
				{
					case "1": CreateAccount(); break;
					case "2": ShowAccounts(); break;
					case "3": DoDeposit(); break;
					case "4": DoWithdraw(); break;
					case "5": DoTransfer(); break;
					case "6": ShowHistory(); break;
					case "7": LoadDemoData(); break;
					case "0": running = false; break;
					default: Console.WriteLine("[Ошибка] Нет такого пункта меню."); break;
				}

				if (running)
				{
					Console.WriteLine("\nНажмите Enter, чтобы продолжить...");
					Console.ReadLine();
				}
			}

			Console.WriteLine("До свидания!");
		}

		static void ShowMenu()
		{
			Console.Clear();
			Console.WriteLine("=== ГЛАВНОЕ МЕНЮ ===");
			Console.WriteLine("1. Открыть счёт");
			Console.WriteLine("2. Показать все счета");
			Console.WriteLine("3. Пополнить счёт");
			Console.WriteLine("4. Снять деньги");
			Console.WriteLine("5. Перевод между счетами");
			Console.WriteLine("6. История операций по счёту");
			Console.WriteLine("7. Добавить демо-счета");
			Console.WriteLine("0. Выход");
			Console.WriteLine("--------------------");
		}

		// ---------- Пункты меню ----------

		static void CreateAccount()
		{
			Console.WriteLine("--- Открытие счёта ---");
			Console.WriteLine("1. Сберегательный (минимальный остаток " + BankAccount.Money(SavingsMinBalance) + ")");
			Console.WriteLine("2. Расчётный (комиссия за снятие " + BankAccount.Money(CheckingFee) + ")");
			string type = ReadLine("Тип счёта: ");

			if (type != "1" && type != "2")
			{
				Console.WriteLine("[Ошибка] Неверный тип счёта.");
				return;
			}

			string owner = ReadLine("Имя владельца: ");
			decimal initial = ReadDecimal("Начальный баланс: ");

			try
			{
				BankAccount account = type == "1"
					? new SavingsAccount(owner, initial, SavingsMinBalance)
					: new CheckingAccount(owner, initial, CheckingFee);

				accounts.Add(account);
				Console.WriteLine($"[Успех] Счёт открыт: {account}");
			}
			catch (ArgumentException ex)
			{
				// Конструкторы бросают исключения при некорректных данных
				Console.WriteLine($"[Ошибка] {ex.Message}");
			}
		}

		static void ShowAccounts()
		{
			Console.WriteLine("--- Список счетов ---");
			if (accounts.Count == 0)
			{
				Console.WriteLine("Счетов пока нет.");
				return;
			}

			foreach (BankAccount account in accounts)
			{
				Console.WriteLine(account);
			}
			Console.WriteLine($"Всего счетов: {accounts.Count}");
		}

		static void DoDeposit()
		{
			Console.WriteLine("--- Пополнение ---");
			BankAccount account = ChooseAccount("Номер счёта: ");
			if (account == null) return;

			decimal amount = ReadDecimal("Сумма пополнения: ");
			account.Deposit(amount);
		}

		static void DoWithdraw()
		{
			Console.WriteLine("--- Снятие ---");
			BankAccount account = ChooseAccount("Номер счёта: ");
			if (account == null) return;

			decimal amount = ReadDecimal("Сумма снятия: ");
			account.Withdraw(amount);   // поведение зависит от типа счёта (полиморфизм)
		}

		static void DoTransfer()
		{
			Console.WriteLine("--- Перевод ---");
			BankAccount from = ChooseAccount("Номер счёта-отправителя: ");
			if (from == null) return;

			BankAccount to = ChooseAccount("Номер счёта-получателя: ");
			if (to == null) return;

			if (from == to)
			{
				Console.WriteLine("[Ошибка] Нельзя переводить на тот же счёт.");
				return;
			}

			decimal amount = ReadDecimal("Сумма перевода: ");

			// Сначала снимаем: если у отправителя правила не позволяют, деньги не уйдут
			if (from.Withdraw(amount))
			{
				to.Deposit(amount);
				Console.WriteLine($"[Перевод] {BankAccount.Money(amount)}: №{from.Number} → №{to.Number}");
			}
			else
			{
				Console.WriteLine("[Перевод отменён]");
			}
		}

		static void ShowHistory()
		{
			Console.WriteLine("--- История операций ---");
			BankAccount account = ChooseAccount("Номер счёта: ");
			if (account == null) return;

			Console.WriteLine($"Счёт №{account.Number} ({account.TypeName}), владелец: {account.Owner}");
			foreach (string line in account.History)
			{
				Console.WriteLine("  " + line);
			}
		}

		static void LoadDemoData()
		{
			accounts.Add(new SavingsAccount("Айбек", 5000m, SavingsMinBalance));
			accounts.Add(new CheckingAccount("Айгерим", 3000m, CheckingFee));
			Console.WriteLine("[Успех] Добавлены 2 демо-счёта.");
			ShowAccounts();
		}

		// ---------- Вспомогательные методы ввода ----------

		static string ReadLine(string prompt)
		{
			Console.Write(prompt);
			return (Console.ReadLine() ?? "").Trim();
		}

		// Повторяет запрос, пока пользователь не введёт корректное число
		static decimal ReadDecimal(string prompt)
		{
			while (true)
			{
				string input = ReadLine(prompt);

				// Принимаем и запятую, и точку в качестве разделителя
				if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal value) ||
					decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
				{
					return value;
				}

				Console.WriteLine("[Ошибка] Введите число (например, 1500 или 250,50).");
			}
		}

		// Запрашивает номер счёта и ищет его в списке; null, если не найден
		static BankAccount ChooseAccount(string prompt)
		{
			if (accounts.Count == 0)
			{
				Console.WriteLine("Счетов пока нет. Сначала откройте счёт (пункт 1 или 7).");
				return null;
			}

			foreach (BankAccount a in accounts)
			{
				Console.WriteLine("  " + a);
			}

			string input = ReadLine(prompt);
			if (!int.TryParse(input, out int number))
			{
				Console.WriteLine("[Ошибка] Номер счёта должен быть числом.");
				return null;
			}

			BankAccount found = accounts.Find(a => a.Number == number);
			if (found == null)
			{
				Console.WriteLine("[Ошибка] Счёт с таким номером не найден.");
			}
			return found;
		}
	}
}
