{-# LANGUAGE ScopedTypeVariables #-}
import qualified Data.ByteString.Char8 as C
import qualified Text.Hex as TH
import qualified Data.Text as DT
import qualified Data.ByteString as BS
import ImprovedParser
import Control.Monad
import Control.Applicative
import System.IO
import Data.List.Split (splitOn)
import System.Environment
import System.Directory
import Data.Char (isAsciiLower, isAsciiUpper, isDigit,toUpper,isUpper,isLower, isSpace)
import Control.Exception
import System.Exit
import System.FilePath(takeDirectory)
import Text.ParserCombinators.ReadP (skipSpaces)
import Foreign (ptrToIntPtr)


iotest::Bool->String->IO String
iotest b hpath =
  do
     tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
     check <- checkMonadSpam hpath
     putStrLn "Parsing..."
     (ptr,isBom) <- parsePath hpath
     if not$ eqTree ptr (Node ("грешка","") []) then do
       putStrLn "Parsing Successful"
       if b then do
          putStrLn "Commands are:\n Set(path,value) - set argpath argvalue \n Get(path) - get argpath \n Remove(path) - remove path \n Q - exit \n CD path - change directory \n PWD - current directory"
          iotest False hpath
       else do
       arg<-getLine
       if map toUpper arg=="Q" then return "Exit"
       else if map toUpper (head (cdfix arg)) =="CD" then do
        let p = head $ tail $cdfix arg
        putStrLn $ "Command: CD \n| argpath = "++p++"\n"
        check<-checkMonadSingle p
        if check then do
        iotest False p
        else do
          putStrLn "Not a valid path"
          iotest False hpath
       else if map toUpper arg=="PWD" then do
        putStrLn $ "Command: PWD \n |Current Dir:\n"++hpath
        iotest False hpath else do
       let com = trComm arg
       let cs = "-s"
       let get = "-g"
       let rem = "-r"
       if length com == 3 && head com == cs then do
          let arg1 = com!!1
          let arg2 = com!!2
          putStrLn $ "Command: Set \n| argpath = "++arg1++"\n| argvalue = "++arg2
          let carg1= convertSet (arg1,arg2)
          --putStrLn $ show carg1
          let set = uncurry (addToTree ptr) carg1
          if strongEqTree set ptr || not (parseCheck set) then do
                  putStrLn $"Set Failed | Nothing was changed"
                  iotest False hpath
          else do
            checks <- checkMonadSingleDangerous hpath
            if not checks then do die "" else do
            if isBom then do
             BS.writeFile hpath bom
             appendFile hpath $show set
             iotest False hpath
             else
               do
                 (tempName, tempHandle) <- openTempFile tempPath "temp"
                 hPutStr tempHandle $ show set
                 hClose tempHandle
                 renameFile tempName hpath
                 iotest False hpath
        else if length com == 2 then
            if head com == get then do
               putStrLn $ "Command: Get \n| argpath = "++com!!1
               let carg = convertGet $com!!1
               putStrLn $ "Value:"++getTree ptr carg
               iotest False hpath
            else if head com == rem then do
               checks <- checkMonadSingleDangerous hpath
               if not checks then do die "" else do
               putStrLn $ "Command: Remove \n| argpath = "++com!!1
               let carg2 = convertGet $ com!!1
               let r = deleteTree ptr carg2
               if strongEqTree r ptr then do
                  putStrLn "Remove Failed | Nothing was changed"
                  iotest False hpath
               else
                do 
                   if isBom then do
                    BS.writeFile hpath bom
                    appendFile hpath $show r
                    iotest False hpath
                    else do
                       (tempName, tempHandle) <- openTempFile tempPath "temp"
                       hPutStr tempHandle $ show r
                       hClose tempHandle
                       renameFile tempName hpath
                       iotest False hpath

            else do
              putStrLn $ "Not a valid command with "++show (length com-1)++" arguments"
              iotest False hpath
          else do
            putStrLn $"Not a valid command with "++show (length com-1)++" arguments"
            iotest False hpath
        else
          do
             putStrLn "Parsing Failed"
             return "False"
     --writeFile "Settings1.cfg" $show d

strongEqTree::Tree->Tree->Bool
strongEqTree (Node n1 t1) (Node n2 t2) = n1 == n2 && length t1==length t2 && and (zipWith strongEqTree t1 t2)
--s
eqTree (Node n1 t1) (Node n2 t2) = n1 == n2

nodeTrees (Node _ tree) = tree

rootTree (Node root _) = root
rootName (Node (r,_) _) = r
rootValue (Node (_,v) _) = v
testTree = (Node ("Root","") [])

addToTree::Tree->[String]->(RootName,Value)->Tree
addToTree (Node ("Root",_) t) [] ("Root",vvalue) = Node ("Root",vvalue) t
addToTree y@(Node root tree) [] (vname,vvalue) = y
addToTree y@(Node rv@(root,value) tree) [x] (vname,vvalue)
  | root == x && null searchTree= Node rv $filtered++[Node (vname,vvalue) []]
  | root == x && not (null searchTree) = Node rv filtered
  |otherwise = y
  where
      searchTree = [Node r t | (Node r@(n,v) t)<-tree,vname== n]
      filtered = map (\x->if not$ eqTree x (Node (vname,rootValue x) []) then x else (Node (vname,vvalue) (nodeTrees x))) tree
addToTree z@(Node rv@(root,value) tree) (x:y:xs) cv@(vname,vvalue)
  | root == x = if null search then addToTree (Node rv (tree++[Node (y,"") []])) (x:y:xs) cv else Node rv filtered
  |otherwise = z
   where
      search = [Node (r,b) t|(Node (r,b) t)<-tree, r==y]
      filtered = map (\x->if eqTree x (Node (y,rootValue x) []) then addToTree x (y:xs) cv else x) tree


convertSet1 (t,v) = (init split,(l,v))
  where
    split = splitOn "/" t
    l = last split

rsplitOn delim str= case sp of
                              [a,b,c]->[a,b,c]
                              [a,b]->[a,b,""]
                              [a]->[a,"",""]
                              []->["","",""]
  where sp = splitOn delim str

convertSet :: ([Char], b) -> ([[Char]], ([Char], b))
convertSet (t,v) = convertSet1 (t,v) 



convertGet  = splitOn "/"

getTree (Node (root,v) _) [x] 
 |root==x = v
 |otherwise = "Err:No value found"
getTree (Node (root,_) tree) (x:y:xs)
  |root == x && not (null filtNodes) = getTree (head filtNodes) (y:xs)
  |otherwise = "Err:No value found"
    where
      filtNodes = [Node (r,v) t|(Node (r,v) t)<-tree,r==y]


deleteTree1::Tree->[String]->Tree
deleteTree1 q [x] = q
deleteTree1 a@(Node randv@(root,vvalue) tree) [x,y]
  |root == x && not (null filtNodes) = Node randv filteredNodes
  |otherwise = a
    where
     filtNodes = [Node (r,v) t|(Node (r,v) t)<-tree,r==y]
     filteredNodes = filter (\x->not $eqTree (Node (fst $rootTree x,"") []) (Node (y,"") [])) tree
deleteTree1 a@(Node randv@(root,_) tree) (x:y:xs)
 |x == root = Node randv (map (\x->deleteTree1 x (y:xs)) tree)
 |otherwise = a

deleteTree::Tree->[String]->Tree
deleteTree a@(Node root tree) b = newTree
 where
  newTree@(Node a1 b2) = deleteTree1 a b

openMonad :: FilePath -> IO (Maybe Handle)
openMonad path = handle (\(e::IOException) ->do
  putStrLn $ displayException e
  return Nothing) $ do
  h <- openFile path ReadMode 
  return (Just h)

openMonadDangerous :: FilePath -> IO (Maybe Handle)
openMonadDangerous path = handle (\(e::IOException) ->do
  putStrLn $ displayException e
  return Nothing) $ do
  h <- openFile path ReadWriteMode
  return (Just h)

checkMonadSingleDangerous :: String -> IO Bool
checkMonadSingleDangerous path = do
     tryopen <- openMonadDangerous path
     case tryopen of
      Nothing -> return False
      (Just h) ->do
         hClose h
         return True

checkMonadSingle :: String -> IO Bool
checkMonadSingle path = do
     tryopen <- openMonad path
     case tryopen of
      Nothing -> return False
      (Just h) ->do
         hClose h
         return True

checkMonadSpam :: String -> IO Bool
checkMonadSpam path = do
     tryopen <- openMonad path
     case tryopen of
      Nothing -> do
        putStrLn "File not found | Please write a valid path or close the program:"
        p <- getLine
        checkMonadSpam p
      (Just h) ->do
         hClose h
         return True
mhm::[String]->IO () --microHandleMonad
mhm com@[r,p,path]
 |r=="-r" || r=="--remove" = do
               check <- checkMonadSingleDangerous path
               if not check then die $"Exit with Error: "
               else do
               (ptr,isBom) <- parsePath path
               if eqTree ptr (Node ("грешка","") []) then die "Exit with Code (100) - Parse Error"
               else do
               let carg2 = convertGet p
               let r = deleteTree ptr carg2
               if strongEqTree r ptr then
                die "Exit with Code (201) - Nothing was changed" --Remove changed nothing
               else do
                 let tempPath = sp path
                
                 if isBom then 
                  do
                    BS.writeFile path bom
                    appendFile path $show r
                  else do

                       (tempName, tempHandle) <- openTempFile tempPath "temp"
                       hPutStr tempHandle $ show r
                       hClose tempHandle
                       renameFile tempName path

                

mhm com@[c,p,value,path]
  |c=="-s" || c=="--set" = do
               check <- checkMonadSingleDangerous path
               if not check then die "Exit with Error: "
               else do
               (ptr,isBom) <- parsePath path
               if eqTree ptr (Node ("грешка","") []) then die "Exit with Code (100) - Parse Error"
               else do
               let carg1= convertSet (p,value)
               --putStrLn $ p++","++value++"|"++show ptr
               let set = uncurry (addToTree ptr) carg1
               if not (parseCheck set) then do
                die "Set failed , nothing was changed"
               else do
               let tempPath =sp path
              
               if isBom then do
                    BS.writeFile path bom
                    appendFile path $show set
                  else do
                    (tempName, tempHandle) <- openTempFile tempPath "temp"
                    hPutStr tempHandle $ show set
                    hClose tempHandle
                    renameFile tempName path
  |otherwise = die "If you get to this error you are insane"
mhm com  = die $"Set didn't have the right arguments"++concatMap ("\n"++) com
main:: IO String
main =
  do
       args<-getArgs
       case args
          of
            com@[path] -> do 
              parsePath path
              return "Yeah buddy"
            com@[command,path,value,cdd]
             |com `elem` [["-s",path,value,cdd],["--set",path,value,cdd]]->
              do
                mhm com
                return "Spawned"

            com@[command,path,value]
             |com `elem` [["-s",path,value],["--set",path,value]] ->
             do
               hcodepath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld\\Settings.cfg"
               tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
               check <- checkMonadSingle hcodepath
               if not check then die "No Settings file found in AppData:\n"
               else do
               (ptr,isBom) <- parsePath hcodepath --
               checks <- checkMonadSingleDangerous hcodepath
               if not checks then do die "" else do
               let carg1= convertSet (path,value)
               let set = uncurry (addToTree ptr) carg1
               if not (parseCheck set) then die "Set failed nothing was changed" else do
               if isBom then do
                     BS.writeFile hcodepath bom
                     appendFile hcodepath $show set
                     return "Spawned"
                  else do
                     (tempName, tempHandle) <- openTempFile tempPath "temp"
                     hPutStr tempHandle $ show set
                     hClose tempHandle
                     renameFile tempName hcodepath
                     return "Spawned"
              |com `elem` [["-g",path,value],["--get",path,value]] ->
              do
                check1 <- checkMonadSingle value
                if not check1 then die "Exit with Error: "
                else
                  do
                    (ptr1,isBom) <- parsePath value
                    let cs = convertGet path
                    --putStrLn $ show $ convertGet path
                    if getTree ptr1 cs == "Err:No value found" then die "Exit with Code (301) - Get returned nothing"-- !vfound
                    else do
                      putStrLn $ getTree ptr1 cs
                      return "End"
              |com `elem` [["-r",path,value],["--remove",path,value]] -> do
                mhm com
                return "End"

              |otherwise -> if command `elem` ["-s","--set"] then die "Exit with Code (101) - wrong number of parameters" -- !#par
               else
                die "Exit with Code (102) - Wrong Command"
            com@[command,path]
             |com `elem` [["-r",path],["--remove",path]] ->
               do
                 hcodepath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld\\Settings.cfg"
                 tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
                 check <- checkMonadSingle hcodepath
                 if not check then die "No Settings file found in AppData:\n"
                 else do
                 (ptr,isBom) <- parsePath hcodepath --
                 checks <- checkMonadSingleDangerous hcodepath
                 if not checks then do die "" else do
                 let carg2 = convertGet path
                 let r = deleteTree ptr carg2
                 if strongEqTree r ptr then
                  die "Exit with Code (201) - Nothing was changed" --Remove changed nothing
                 else do
                  hcodepath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld\\Settings.cfg"
                  tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
                  check <- checkMonadSingle hcodepath
                  if not check then die "No Settings file found in AppData:\n"
                  else do
                  (ptr,isBom) <- parsePath hcodepath --
                  if isBom then do
                     BS.writeFile hcodepath bom
                     appendFile hcodepath $show r
                     return "Deded"
                  else do
                     (tempName, tempHandle) <- openTempFile tempPath "temp"
                     hPutStr tempHandle $ show r
                     hClose tempHandle
                     renameFile tempName hcodepath --
                     return "Deded"
             |com `elem` [["-g",path],["--get",path]] ->
              do
                  hcodepath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld\\Settings.cfg"
                  tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
                  check <- checkMonadSingle hcodepath
                  if not check then die "No Settings file found in AppData:\n"
                  else do
                  (ptr,isBom) <- parsePath hcodepath --
                  let carg = convertGet path
                  if getTree ptr carg == "Err:No value found" then die "Exit with Code (301) - Get returned nothing"-- !vfound
                  else do
                   putStrLn $ getTree ptr carg
                   return "GetLow"
             |otherwise -> die "Exit with Code (102) - Wrong Command"

            [] -> do 
                   hcodepath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld\\Settings.cfg"
                   tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
                   check <- checkMonadSingle hcodepath
                   if not check then die "No Settings file found in AppData:\n"
                   else do
                   (ptr,isBom) <- parsePath hcodepath --
                   if not$ eqTree ptr (Node ("грешка","") []) then do
                      putStrLn hcodepath
                      iotest True hcodepath
                      return "reworked"
                   else return "lol"
            _ -> die "Exit with Code (666) No valid arguments"
              --unparsedPart filt



--Set("path","value")
--Remove("path")
--Get("path")

trComm::String->[String]
trComm str = case com of
   "SET" -> "-s":args
   "GET" -> "-g":args
   "REMOVE" -> "-r":args
   _ -> ["Error"]
  where
    [t,d] = map ($str) [takeWhile (/='('),dropWhile (/='(') . takeWhile (/=')')]
    remwhite = concat . words
    com = remwhite $ map toUpper t
    args = splitOn ","  $ tail d

cdfix str =if null rest || null (tail rest) then [""] else [cd,tail rest]
  where
    (cd,rest) = splitAt 2 str

parseCheck tree = True




sp  = takeDirectory
